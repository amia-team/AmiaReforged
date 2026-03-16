using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to craft an item using a recipe
/// </summary>
public record CraftItemCommand : ICommand
{
    /// <summary>
    /// Character attempting to craft
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Industry containing the recipe
    /// </summary>
    public required IndustryTag IndustryTag { get; init; }

    /// <summary>
    /// Recipe to execute
    /// </summary>
    public required RecipeId RecipeId { get; init; }

    /// <summary>
    /// Optional: Additional context for industry-specific crafting logic
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();

    /// <summary>
    /// Qualities of the input items selected by the player, one per ingredient slot.
    /// Null entries indicate ingredients with no quality.
    /// Used to compute the base quality of crafted products.
    /// </summary>
    public List<int?> InputQualities { get; init; } = [];
}

/// <summary>
/// Handles crafting items from recipes
/// </summary>
[ServiceBinding(typeof(ICommandHandler<CraftItemCommand>))]
public class CraftItemHandler : ICommandHandler<CraftItemCommand>
{
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly ICraftingProcessor _craftingProcessor;
    private readonly IKnowledgeProgressionService _progressionService;
    private readonly RecipeTemplateExpander _templateExpander;

    public CraftItemHandler(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICraftingProcessor craftingProcessor,
        IKnowledgeProgressionService progressionService,
        RecipeTemplateExpander templateExpander)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _craftingProcessor = craftingProcessor;
        _progressionService = progressionService;
        _templateExpander = templateExpander;
    }

    public async Task<CommandResult> HandleAsync(CraftItemCommand command,
        CancellationToken cancellationToken = default)
    {
        // Get industry and recipe
        Industry? industry = _industryRepository.GetByTag(command.IndustryTag);
        if (industry == null)
        {
            return CommandResult.Fail($"Industry '{command.IndustryTag.Value}' not found");
        }

        Recipe? recipe = industry.Recipes.FirstOrDefault(r => r.RecipeId == command.RecipeId);

        // Fall back to template-expanded recipes (cached in memory by RecipeTemplateExpander)
        recipe ??= _templateExpander.GetExpandedRecipes(command.IndustryTag)
            .FirstOrDefault(r => r.RecipeId == command.RecipeId);

        if (recipe == null)
        {
            return CommandResult.Fail(
                $"Recipe '{command.RecipeId.Value}' not found in industry '{command.IndustryTag.Value}'");
        }

        // Check character membership
        List<IndustryMembership> memberships = _membershipRepository.All(command.CharacterId.Value);
        IndustryMembership? membership =
            memberships.FirstOrDefault(m => m.IndustryTag.Value == command.IndustryTag.Value);
        if (membership == null)
        {
            return CommandResult.Fail($"Character is not a member of industry '{industry.Name}'");
        }

        // Check required knowledge
        List<Knowledge> characterKnowledge = _knowledgeRepository.GetAllKnowledge(command.CharacterId.Value);
        List<string> missingKnowledge = recipe.RequiredKnowledge
            .Where(req => !characterKnowledge.Any(ck => ck.Tag == req))
            .ToList();

        if (missingKnowledge.Any())
        {
            return CommandResult.Fail($"Missing required knowledge: {string.Join(", ", missingKnowledge)}");
        }

        // Aggregate crafting modifiers from character knowledge
        List<CraftingModifier> rawModifiers = characterKnowledge
            .SelectMany(k => k.CraftingModifiers)
            .Where(m => m.Matches(recipe.RecipeId.Value, command.IndustryTag.Value))
            .ToList();
        AggregatedCraftingModifiers modifiers = AggregatedCraftingModifiers.Aggregate(rawModifiers);

        // Compute base quality from input ingredient qualities
        int baseQuality = CraftingQuality.ComputeBaseQuality(command.InputQualities);

        // Execute crafting process (industry-specific logic via processor)
        CraftingResult craftingResult =
            await _craftingProcessor.ProcessCraftingAsync(command.CharacterId, recipe, baseQuality, modifiers, command.Context);

        if (!craftingResult.Success)
        {
            return CommandResult.Fail(craftingResult.Message);
        }

        // Award progression points if successful
        if (craftingResult.ProgressionPointsAwarded > 0)
        {
            ProgressionResult progressionResult =
                _progressionService.AwardProgressionPoints(command.CharacterId, craftingResult.ProgressionPointsAwarded);

            if (progressionResult is { Success: true, KnowledgePointsEarned: > 0 })
            {
                // Return progression info in the command result data
                return CommandResult.Ok(new Dictionary<string, object>
                {
                    ["knowledgePointsEarned"] = progressionResult.KnowledgePointsEarned,
                    ["newTotalKnowledgePoints"] = progressionResult.NewTotalKnowledgePoints,
                    ["progressionPointsRemaining"] = progressionResult.ProgressionPointsRemaining,
                    ["progressionPointsRequired"] = progressionResult.ProgressionPointsRequired,
                    ["isAtSoftCap"] = progressionResult.IsAtSoftCap,
                    ["isAtHardCap"] = progressionResult.IsAtHardCap,
                    ["message"] = progressionResult.Message ?? string.Empty
                });
            }
        }

        return CommandResult.Ok();
    }
}

/// <summary>
/// Interface for industry-specific crafting logic.
/// Different industries can implement their own processors.
/// </summary>
public interface ICraftingProcessor
{
    Task<CraftingResult> ProcessCraftingAsync(CharacterId characterId, Recipe recipe,
        int baseQuality, AggregatedCraftingModifiers modifiers, Dictionary<string, object> context);
}

/// <summary>
/// Default crafting processor for generic crafting.
/// Applies <see cref="AggregatedCraftingModifiers"/> to the recipe's products.
/// </summary>
[ServiceBinding(typeof(ICraftingProcessor))]
public class DefaultCraftingProcessor : ICraftingProcessor
{
    public Task<CraftingResult> ProcessCraftingAsync(CharacterId characterId, Recipe recipe,
        int baseQuality, AggregatedCraftingModifiers modifiers, Dictionary<string, object> context)
    {
        // Compute output quality: base (from inputs) + knowledge bonus, clamped to craftable range
        int outputQuality = CraftingQuality.Clamp(baseQuality + modifiers.QualityBonus);

        // Apply modifiers to products
        List<Product> modifiedProducts = recipe.Products.Select(p =>
        {
            // Quantity: multiply and floor, minimum 1
            int quantity = Math.Max(1, (int)Math.Floor(p.Quantity.Value * modifiers.QuantityMultiplier));

            // Success chance: add bonus, clamp to [0.0, 1.0]
            float? successChance = p.SuccessChance.HasValue
                ? Math.Clamp(p.SuccessChance.Value + modifiers.SuccessChanceBonus, 0f, 1f)
                : null;

            return new Product
            {
                ItemTag = p.ItemTag,
                Quantity = Quantity.Parse(quantity),
                Quality = outputQuality,
                SuccessChance = successChance
            };
        }).ToList();

        CraftingResult result = new CraftingResult
        {
            Success = true,
            Message = "Crafting completed successfully",
            ProductsCreated = modifiedProducts,
            IngredientsConsumed = recipe.Ingredients.ToList(),
            ProgressionPointsAwarded = recipe.ProgressionPointsAwarded
        };

        return Task.FromResult(result);
    }
}
