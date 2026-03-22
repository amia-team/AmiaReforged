using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// Presenter for the workstation crafting NUI window.
/// Handles recipe list display, search filtering, pagination, detail display,
/// and placeholder craft action. Auto-closes when the player moves away.
/// </summary>
public sealed class WorkstationCraftingPresenter : ScryPresenter<WorkstationCraftingView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly WorkstationTag _workstationTag;
    private readonly string _workstationName;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private WorkstationCraftingModel? _model;

    /// <summary>Tracks selected quality tier per ingredient index for the current recipe.</summary>
    private readonly Dictionary<int, int> _selectedQualities = new();

    /// <summary>Cached ingredient slot info for the current recipe selection.</summary>
    private List<(string Tag, string Name, int Qty, List<int> AvailableQualities)> _ingredientSlots = [];

    [Inject] private Lazy<IIndustrySubsystem>? IndustrySubsystem { get; init; }
    [Inject] private Lazy<RuntimeCharacterService>? CharacterService { get; init; }
    [Inject] private Lazy<ICharacterRepository>? CharacterRepository { get; init; }
    [Inject] private Lazy<WindowDirector>? Director { get; init; }

    public WorkstationCraftingPresenter(
        WorkstationCraftingView view,
        NwPlayer player,
        WorkstationTag workstationTag,
        string workstationName)
    {
        View = view;
        _player = player;
        _workstationTag = workstationTag;
        _workstationName = workstationName;
    }

    public override WorkstationCraftingView View { get; }
    public override NuiWindowToken Token() => _token;

    // IAutoCloseOnMove defaults: 1s poll, 0.1m threshold

    public override void InitBefore()
    {
        _model = new WorkstationCraftingModel(_workstationTag, _workstationName);
        _window = new NuiWindow(View.RootLayout(), _workstationName)
        {
            Geometry = new NuiRect(40f, 40f, WorkstationCraftingView.WindowW, WorkstationCraftingView.WindowH),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Workstation window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open workstation window.", ColorConstants.Orange);
            return;
        }

        // Resolve character ID
        CharacterId? characterId = ResolveCharacterId();
        if (characterId == null)
        {
            _player.SendServerMessage("No character key found.", ColorConstants.Orange);
            SetDetailContent("Error", "No character key found. Ensure you have a valid character.");
            return;
        }

        // Load recipes
        List<Recipe> recipes;
        try
        {
            recipes = IndustrySubsystem!.Value
                .GetWorkstationRecipesAsync(characterId.Value, _workstationTag)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load workstation recipes for {Tag}", _workstationTag.Value);
            recipes = [];
        }

        _model!.Load(recipes);

        // Set up search watch
        _token.SetBindWatch(View.SearchText, true);
        _token.SetBindValue(View.SearchText, string.Empty);

        // Initial UI state
        RefreshEntryList();
        SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
        _token.SetBindValue(View.ShowCraftButton, false);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        // Handle search watch
        if (eventData.EventType == NuiEventType.Watch && eventData.ElementId == View.SearchText.Key)
        {
            string searchTerm = (_token.GetBindValue(View.SearchText) ?? string.Empty).Trim();
            _model!.ApplySearch(searchTerm);
            RefreshEntryList();
            SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
            _token.SetBindValue(View.ShowCraftButton, false);
            return;
        }

        // Handle ingredient quality combo selection changes
        if (eventData.EventType == NuiEventType.Watch)
        {
            for (int i = 0; i < _ingredientSlots.Count && i < WorkstationCraftingView.MaxIngredientSlots; i++)
            {
                if (eventData.ElementId == View.IngredientQualitySelected[i].Key)
                {
                    int selected = _token.GetBindValue(View.IngredientQualitySelected[i]);
                    _selectedQualities[i] = selected;
                    return;
                }
            }
        }

        if (eventData.EventType != NuiEventType.Click) return;
        HandleClick(eventData.ElementId);
    }

    public override void Close()
    {
        try { _token.Close(); } catch { /* token may already be invalid */ }
    }

    // --- Click Handling ---

    private void HandleClick(string elementId)
    {
        switch (elementId)
        {
            case "btn_close":
                RaiseCloseEvent();
                Close();
                break;

            case "btn_clear_search":
                _token.SetBindValue(View.SearchText, string.Empty);
                _model!.ApplySearch(string.Empty);
                RefreshEntryList();
                SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
                _token.SetBindValue(View.ShowCraftButton, false);
                break;

            case "btn_prev_page":
                if (_model!.CurrentPage > 0)
                {
                    _model.CurrentPage--;
                    RefreshEntryList();
                }
                break;

            case "btn_next_page":
                if (_model!.CurrentPage < _model.TotalPages - 1)
                {
                    _model.CurrentPage++;
                    RefreshEntryList();
                }
                break;

            case "btn_craft":
                HandleCraft();
                break;

            default:
                if (elementId.StartsWith("btn_recipe_") && int.TryParse(elementId["btn_recipe_".Length..], out int rowIndex))
                    SelectEntry(rowIndex);
                break;
        }
    }

    // --- Entry List ---

    private void RefreshEntryList()
    {
        IReadOnlyList<Recipe> pageRecipes = _model!.CurrentPageRecipes;

        _token.SetBindValue(View.PageInfo, $"{_model.CurrentPage + 1} / {_model.TotalPages}");
        _token.SetBindValue(View.ShowPrevPage, _model.CurrentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _model.CurrentPage < _model.TotalPages - 1 && _model.FilteredRecipes.Count > 0);

        for (int i = 0; i < WorkstationCraftingView.EntriesPerPage; i++)
        {
            if (i < pageRecipes.Count)
            {
                Recipe recipe = pageRecipes[i];
                _token.SetBindValue(View.EntryNames[i], recipe.Name);
                _token.SetBindValue(View.EntrySubtitles[i], FormatSubtitle(recipe));
                _token.SetBindValue(View.EntryRowVisible[i], true);
            }
            else
            {
                _token.SetBindValue(View.EntryRowVisible[i], false);
            }
        }
    }

    private static string FormatSubtitle(Recipe recipe)
    {
        int ingredientCount = recipe.Ingredients.Count;
        return $"{ingredientCount} ingredient{(ingredientCount != 1 ? "s" : "")}";
    }

    // --- Selection & Detail ---

    private void SelectEntry(int rowIndex)
    {
        int absoluteIndex = (_model!.CurrentPage * WorkstationCraftingView.EntriesPerPage) + rowIndex;
        Recipe? recipe = _model.GetRecipeAt(absoluteIndex);
        if (recipe == null) return;

        _model.SelectedIndex = absoluteIndex;

        // Resolve character context for knowledge modifiers
        AggregatedCraftingModifiers modifiers = AggregatedCraftingModifiers.None;
        CharacterId? characterId = ResolveCharacterId();
        if (characterId != null && CharacterRepository?.Value != null)
        {
            ICharacter? character = CharacterRepository.Value.GetById(characterId.Value.Value);
            if (character != null)
            {
                List<CraftingModifier> rawModifiers = character.CraftingModifiersForRecipe(
                    recipe.RecipeId.Value, recipe.IndustryTag.Value);
                modifiers = AggregatedCraftingModifiers.Aggregate(rawModifiers);
            }
        }

        // Scan inventory for per-ingredient quality tiers
        _ingredientSlots = ScanIngredientQualities(recipe);
        _selectedQualities.Clear();

        // Build interactive detail layout with quality combos
        NuiColumn detailLayout = View.BuildRecipeDetailLayout(recipe, _ingredientSlots);
        _token.SetGroupLayout(View.DetailGroup, detailLayout);

        // Set body text (without ingredients — shown as combos in layout)
        string body = FormatRecipeBodyWithoutIngredients(recipe, modifiers);
        SetDetailContent(recipe.Name, body);

        // Populate combo binds and default selections
        for (int i = 0; i < _ingredientSlots.Count && i < WorkstationCraftingView.MaxIngredientSlots; i++)
        {
            List<int> qualities = _ingredientSlots[i].AvailableQualities;
            if (qualities.Count <= 1)
            {
                // Single or no quality — store the default
                _selectedQualities[i] = qualities.Count == 1 ? qualities[0] : CraftingQuality.Unknown;
                continue;
            }

            // Populate combo entries
            List<NuiComboEntry> entries = qualities
                .Select(q => new NuiComboEntry(CraftingQuality.Label(q), q))
                .ToList();
            _token.SetBindValue(View.IngredientQualityOptions[i], entries);
            _token.SetBindValue(View.IngredientQualitySelected[i], qualities[0]);
            _token.SetBindWatch(View.IngredientQualitySelected[i], true);
            _selectedQualities[i] = qualities[0];
        }

        _token.SetBindValue(View.ShowCraftButton, true);
    }

    /// <summary>
    /// Formats the recipe detail body text WITHOUT the ingredients section.
    /// Ingredients are shown as interactive combo widgets in the detail pane layout.
    /// </summary>
    private static string FormatRecipeBodyWithoutIngredients(Recipe recipe, AggregatedCraftingModifiers modifiers)
    {
        List<string> sections = [];

        // Description
        if (!string.IsNullOrWhiteSpace(recipe.Description))
            sections.Add(recipe.Description);

        // Industry & Knowledge
        sections.Add($"Industry: {recipe.IndustryTag.Value}");
        if (recipe.RequiredKnowledge.Count > 0)
            sections.Add($"Required Knowledge: {string.Join(", ", recipe.RequiredKnowledge)}");

        // Products
        if (recipe.Products.Count > 0)
        {
            sections.Add("\n--- Products ---");
            foreach (Product product in recipe.Products)
            {
                string chance = product.SuccessChance.HasValue ? $" ({product.SuccessChance:P0} chance)" : "";
                sections.Add($"  {product.Quantity.Value}x {product.ItemTag}{chance}");
            }

            sections.Add("  Quality: determined by input quality + knowledge");
        }

        // Crafting time (with modifier adjustment)
        if (recipe.CraftingTimeRounds.HasValue)
        {
            int baseRounds = recipe.CraftingTimeRounds.Value;
            int effectiveRounds = Math.Max(1, baseRounds - modifiers.TimeReductionRounds);
            string timeText = modifiers.TimeReductionRounds > 0
                ? $"\nCrafting Time: {effectiveRounds} rounds (base {baseRounds}, -{modifiers.TimeReductionRounds} from knowledge)"
                : $"\nCrafting Time: {effectiveRounds} rounds";
            sections.Add(timeText);
        }

        // Knowledge modifier summary
        if (!modifiers.IsEmpty)
        {
            sections.Add("\n--- Knowledge Bonuses ---");
            if (modifiers.QualityBonus != 0)
                sections.Add($"  Quality: {(modifiers.QualityBonus > 0 ? "+" : "")}{modifiers.QualityBonus} tiers");
            if (Math.Abs(modifiers.QuantityMultiplier - 1.0f) > 0.001f)
                sections.Add($"  Quantity: x{modifiers.QuantityMultiplier:F2}");
            if (modifiers.SuccessChanceBonus != 0)
                sections.Add($"  Success Chance: {(modifiers.SuccessChanceBonus > 0 ? "+" : "")}{modifiers.SuccessChanceBonus:P0}");
        }

        // Progression points
        if (recipe.ProgressionPointsAwarded > 0)
            sections.Add($"Progression Points: +{recipe.ProgressionPointsAwarded}");

        return string.Join("\n", sections);
    }

    // --- Craft Action ---

    private void HandleCraft()
    {
        Recipe? recipe = _model?.SelectedRecipe;
        if (recipe == null)
        {
            _player.SendServerMessage("No recipe selected.", ColorConstants.Orange);
            return;
        }

        // Resolve character ID
        CharacterId? characterId = ResolveCharacterId();
        if (characterId == null)
        {
            _player.SendServerMessage("No character key found.", ColorConstants.Orange);
            return;
        }

        // Validate ingredients in player inventory
        string? validationError = ValidateIngredients(recipe);
        if (validationError != null)
        {
            _player.SendServerMessage(validationError, ColorConstants.Orange);
            return;
        }

        // Resolve crafting modifiers from character knowledge
        AggregatedCraftingModifiers modifiers = AggregatedCraftingModifiers.None;
        if (CharacterRepository?.Value != null)
        {
            ICharacter? character = CharacterRepository.Value.GetById(characterId.Value.Value);
            if (character != null)
            {
                List<CraftingModifier> rawModifiers = character.CraftingModifiersForRecipe(
                    recipe.RecipeId.Value, recipe.IndustryTag.Value);
                modifiers = AggregatedCraftingModifiers.Aggregate(rawModifiers);
            }
        }

        // Build selected qualities list in ingredient order
        List<int> selectedQualities = [];
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            selectedQualities.Add(_selectedQualities.GetValueOrDefault(i, CraftingQuality.Unknown));
        }

        // Close the workstation recipe window
        RaiseCloseEvent();
        Close();

        // Open the crafting progress window with selected qualities
        CraftingProgressView progressView = new(_player, recipe, modifiers, characterId.Value, selectedQualities);
        Director!.Value.OpenWindow(progressView.Presenter);
    }

    /// <summary>
    /// Validates that the player has all required ingredients in their inventory,
    /// filtered by the selected quality tier per ingredient.
    /// Returns null if validation passes, or an error message if it fails.
    /// </summary>
    private string? ValidateIngredients(Recipe recipe)
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return "No character found.";

        List<string> missing = [];

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            Ingredient ingredient = recipe.Ingredients[i];
            int selectedQuality = _selectedQualities.GetValueOrDefault(i, CraftingQuality.Unknown);

            // Count matching items with the selected quality tier
            int available = creature.Inventory.Items
                .Where(item => item.Tag == ingredient.ItemTag &&
                               item.GetObjectVariable<LocalVariableInt>("item_quality").Value == selectedQuality)
                .Sum(item => item.StackSize);

            if (available < ingredient.Quantity.Value)
            {
                int deficit = ingredient.Quantity.Value - available;
                string qualLabel = CraftingQuality.Label(selectedQuality);
                missing.Add($"{deficit}x {ingredient.ItemTag} ({qualLabel})");
            }
        }

        return missing.Count > 0
            ? $"Missing ingredients: {string.Join(", ", missing)}"
            : null;
    }

    // --- Helpers ---

    private void SetDetailContent(string title, string body)
    {
        _token.SetBindValue(View.DetailTitle, title);
        _token.SetBindValue(View.DetailBody, body);
    }

    /// <summary>
    /// Scans the player's inventory to find distinct quality tiers available for each ingredient.
    /// Returns per-ingredient slot info: (tag, display name, required quantity, sorted list of available quality tiers).
    /// </summary>
    private List<(string Tag, string Name, int Qty, List<int> AvailableQualities)> ScanIngredientQualities(Recipe recipe)
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return [];

        List<(string Tag, string Name, int Qty, List<int> AvailableQualities)> result = [];

        foreach (Ingredient ingredient in recipe.Ingredients)
        {
            List<NwItem> matchingItems = creature.Inventory.Items
                .Where(item => item.Tag == ingredient.ItemTag)
                .ToList();

            // Use the first matching item's name, falling back to the tag
            string displayName = matchingItems.Count > 0 ? matchingItems[0].Name : ingredient.ItemTag;

            // Group matching items by quality tier, keeping only tiers with enough quantity
            List<int> qualities = matchingItems
                .GroupBy(item => item.GetObjectVariable<LocalVariableInt>("item_quality").Value)
                .Where(g => g.Sum(item => item.StackSize) >= ingredient.Quantity.Value)
                .Select(g => g.Key)
                .OrderBy(q => q)
                .ToList();

            result.Add((ingredient.ItemTag, displayName, ingredient.Quantity.Value, qualities));
        }

        return result;
    }

    private CharacterId? ResolveCharacterId()
    {
        // First try RuntimeCharacterService (preferred — already cached)
        if (CharacterService?.Value != null &&
            CharacterService.Value.TryGetPlayerKey(_player, out Guid cachedKey) &&
            cachedKey != Guid.Empty)
        {
            return CharacterId.From(cachedKey);
        }

        // Fallback: parse from ds_pckey item
        try
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items
                .FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return null;

            string dbToken = pcKey.Name.Split("_")[1];
            if (!Guid.TryParse(dbToken, out Guid guid)) return null;
            return CharacterId.From(guid);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve CharacterId for workstation crafting");
            return null;
        }
    }
}
