using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Expands <see cref="RecipeTemplate"/> definitions into concrete <see cref="Recipe"/> instances
/// by matching item blueprints against the template's material category and form requirements.
/// <para>
/// Concrete recipes are cached in memory. The cache is invalidated and rebuilt when templates
/// or item blueprints change (call <see cref="Invalidate"/>).
/// </para>
/// </summary>
[ServiceBinding(typeof(RecipeTemplateExpander))]
public class RecipeTemplateExpander
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IRecipeTemplateRepository _templateRepository;
    private readonly IItemDefinitionRepository _itemRepository;

    /// <summary>
    /// Cache: template tag → list of expanded concrete recipes.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<Recipe>> _expandedByTemplate = new();

    /// <summary>
    /// Cache: industry tag → list of all expanded concrete recipes across all templates for that industry.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<Recipe>> _expandedByIndustry = new();

    private volatile bool _isExpanded;

    public RecipeTemplateExpander(
        IRecipeTemplateRepository templateRepository,
        IItemDefinitionRepository itemRepository)
    {
        _templateRepository = templateRepository;
        _itemRepository = itemRepository;
    }

    /// <summary>
    /// Expands all recipe templates against all item blueprints and caches the results.
    /// Safe to call multiple times — will skip if already expanded.
    /// </summary>
    public void ExpandAll()
    {
        if (_isExpanded) return;

        Log.Info("Expanding all recipe templates...");

        List<RecipeTemplate> templates = _templateRepository.All();
        List<ItemBlueprint> allBlueprints = _itemRepository.AllItems();

        int totalRecipes = 0;

        foreach (RecipeTemplate template in templates)
        {
            List<Recipe> recipes = ExpandTemplate(template, allBlueprints);
            _expandedByTemplate[template.Tag] = recipes;
            totalRecipes += recipes.Count;
        }

        RebuildIndustryCache();
        _isExpanded = true;

        Log.Info($"Recipe template expansion complete: {templates.Count} templates → {totalRecipes} concrete recipes.");
    }

    /// <summary>
    /// Clears the cache and triggers a full re-expansion.
    /// Call this when templates or item blueprints change.
    /// </summary>
    public void Invalidate()
    {
        _isExpanded = false;
        _expandedByTemplate.Clear();
        _expandedByIndustry.Clear();
        ExpandAll();
    }

    /// <summary>
    /// Gets all expanded concrete recipes for a given industry.
    /// </summary>
    public List<Recipe> GetExpandedRecipes(IndustryTag industryTag)
    {
        EnsureExpanded();
        return _expandedByIndustry.GetValueOrDefault(industryTag.Value, []);
    }

    /// <summary>
    /// Gets all expanded concrete recipes for a given industry filtered by workstation.
    /// </summary>
    public List<Recipe> GetExpandedRecipesForWorkstation(IndustryTag industryTag, WorkstationTag workstationTag)
    {
        return GetExpandedRecipes(industryTag)
            .Where(r => r.RequiredWorkstation != null &&
                        r.RequiredWorkstation.Value.Value == workstationTag.Value)
            .ToList();
    }

    /// <summary>
    /// Gets all expanded concrete recipes from a specific template.
    /// </summary>
    public List<Recipe> GetExpandedRecipesForTemplate(string templateTag)
    {
        EnsureExpanded();
        return _expandedByTemplate.GetValueOrDefault(templateTag, []);
    }

    /// <summary>
    /// Expands a single template against a set of item blueprints.
    /// This is the core expansion algorithm.
    /// </summary>
    public List<Recipe> ExpandTemplate(RecipeTemplate template, List<ItemBlueprint>? blueprints = null)
    {
        blueprints ??= _itemRepository.AllItems();
        List<Recipe> recipes = new List<Recipe>();

        if (template.Ingredients.Count == 0 || template.Products.Count == 0)
        {
            Log.Warn($"Template '{template.Tag}' has no ingredients or products — skipping.");
            return recipes;
        }

        // For each ingredient slot, find all matching blueprints
        List<List<IngredientMatch>> slotMatches = new List<List<IngredientMatch>>();

        foreach (TemplateIngredient ingredient in template.Ingredients.OrderBy(i => i.SlotIndex))
        {
            List<IngredientMatch> matches = FindMatchingBlueprints(blueprints, ingredient);
            if (matches.Count == 0)
            {
                Log.Debug($"Template '{template.Tag}' slot {ingredient.SlotIndex} " +
                          $"({ingredient.RequiredCategory}/{ingredient.RequiredForm}) has no matching blueprints.");
                return recipes; // If any slot has no matches, no recipes can be generated
            }

            slotMatches.Add(matches);
        }

        // Compute cartesian product of all slot matches
        List<List<IngredientMatch>> combinations = CartesianProduct(slotMatches);

        foreach (List<IngredientMatch> combination in combinations)
        {
            Recipe? recipe = TryBuildRecipe(template, combination, blueprints);
            if (recipe != null)
            {
                recipes.Add(recipe);
            }
        }

        Log.Debug($"Template '{template.Tag}' expanded to {recipes.Count} concrete recipes.");
        return recipes;
    }

    // ==================== Private Helpers ====================

    private void EnsureExpanded()
    {
        if (!_isExpanded) ExpandAll();
    }

    private void RebuildIndustryCache()
    {
        _expandedByIndustry.Clear();

        foreach ((string templateTag, List<Recipe> recipes) in _expandedByTemplate)
        {
            foreach (Recipe recipe in recipes)
            {
                string industryKey = recipe.IndustryTag.Value;
                List<Recipe> list = _expandedByIndustry.GetOrAdd(industryKey, _ => new List<Recipe>());
                list.Add(recipe);
            }
        }
    }

    /// <summary>
    /// Finds all blueprints matching a template ingredient's category and form requirements.
    /// </summary>
    private static List<IngredientMatch> FindMatchingBlueprints(
        List<ItemBlueprint> blueprints,
        TemplateIngredient ingredient)
    {
        List<IngredientMatch> matches = new List<IngredientMatch>();

        foreach (ItemBlueprint bp in blueprints)
        {
            // Must match the required form (ItemForm)
            if (bp.ItemForm != ingredient.RequiredForm) continue;

            // Must have at least one material in the required category
            MaterialEnum matchingMaterial = bp.Materials
                .FirstOrDefault(m => m.GetCategory() == ingredient.RequiredCategory);

            if (matchingMaterial == MaterialEnum.None && ingredient.RequiredCategory != MaterialCategory.None)
            {
                // Check if any material matches (None is the default, need explicit check)
                bool hasMatch = bp.Materials.Any(m => m.GetCategory() == ingredient.RequiredCategory);
                if (!hasMatch) continue;
                matchingMaterial = bp.Materials.First(m => m.GetCategory() == ingredient.RequiredCategory);
            }

            if (matchingMaterial.GetCategory() == ingredient.RequiredCategory)
            {
                matches.Add(new IngredientMatch(bp, matchingMaterial, ingredient.SlotIndex));
            }
        }

        return matches;
    }

    /// <summary>
    /// Attempts to build a concrete recipe from a template and a set of resolved ingredient matches.
    /// Returns null if any output product cannot be resolved.
    /// </summary>
    private static Recipe? TryBuildRecipe(
        RecipeTemplate template,
        List<IngredientMatch> ingredientMatches,
        List<ItemBlueprint> allBlueprints)
    {
        // Build concrete ingredients
        List<Ingredient> concreteIngredients = new List<Ingredient>();
        foreach (IngredientMatch match in ingredientMatches)
        {
            TemplateIngredient templateIngredient = template.Ingredients
                .First(i => i.SlotIndex == match.SlotIndex);

            concreteIngredients.Add(new Ingredient
            {
                ItemTag = match.Blueprint.ItemTag,
                Quantity = templateIngredient.Quantity,
                MinQuality = templateIngredient.MinQuality,
                IsConsumed = templateIngredient.IsConsumed
            });
        }

        // Build concrete products
        List<Product> concreteProducts = new List<Product>();
        foreach (TemplateProduct templateProduct in template.Products)
        {
            // Find the ingredient match at the source slot to get the resolved material
            IngredientMatch? sourceMatch = ingredientMatches
                .FirstOrDefault(m => m.SlotIndex == templateProduct.MaterialSourceSlot);

            if (sourceMatch == null)
            {
                Log.Warn($"Template '{template.Tag}' product references slot {templateProduct.MaterialSourceSlot} " +
                         "which has no match — skipping this combination.");
                return null;
            }

            // Find a blueprint matching the output form + the resolved material
            ItemBlueprint? outputBlueprint = allBlueprints.FirstOrDefault(bp =>
                bp.ItemForm == templateProduct.OutputForm &&
                bp.Materials.Contains(sourceMatch.ResolvedMaterial));

            if (outputBlueprint == null)
            {
                // No matching output blueprint — this combination is invalid
                return null;
            }

            concreteProducts.Add(new Product
            {
                ItemTag = outputBlueprint.ItemTag,
                Quantity = templateProduct.Quantity,
                SuccessChance = templateProduct.SuccessChance
            });
        }

        // Build a deterministic recipe ID from the template tag and material combo
        string materialSuffix = string.Join("_",
            ingredientMatches.OrderBy(m => m.SlotIndex).Select(m => m.ResolvedMaterial.ToString().ToLowerInvariant()));

        string recipeId = $"{template.Tag}_{materialSuffix}";

        // Build a descriptive name from the product(s)
        string recipeName = concreteProducts.Count == 1
            ? allBlueprints.FirstOrDefault(bp => bp.ItemTag == concreteProducts[0].ItemTag)?.Name ?? template.Name
            : template.Name;

        return new Recipe
        {
            RecipeId = new RecipeId(recipeId),
            Name = recipeName,
            Description = template.Description,
            IndustryTag = template.IndustryTag,
            RequiredKnowledge = template.RequiredKnowledge,
            Ingredients = concreteIngredients,
            Products = concreteProducts,
            CraftingTimeRounds = template.CraftingTimeRounds,
            KnowledgePointsAwarded = template.KnowledgePointsAwarded,
            Metadata = new Dictionary<string, object>(template.Metadata)
            {
                ["_sourceTemplate"] = template.Tag
            },
            RequiredWorkstation = template.RequiredWorkstation,
            RequiredTools = template.RequiredTools
        };
    }

    /// <summary>
    /// Computes the cartesian product of multiple lists of ingredient matches.
    /// Each result is one valid combination of ingredient matches across all slots.
    /// </summary>
    private static List<List<IngredientMatch>> CartesianProduct(List<List<IngredientMatch>> slotMatches)
    {
        if (slotMatches.Count == 0) return [];

        IEnumerable<List<IngredientMatch>> result = [[]];

        foreach (List<IngredientMatch> slotOptions in slotMatches)
        {
            result = result.SelectMany(
                combo => slotOptions,
                (combo, match) => new List<IngredientMatch>(combo) { match });
        }

        return result.ToList();
    }

    /// <summary>
    /// Represents a resolved match between a template ingredient slot and a concrete item blueprint.
    /// </summary>
    private record IngredientMatch(
        ItemBlueprint Blueprint,
        MaterialEnum ResolvedMaterial,
        int SlotIndex);
}
