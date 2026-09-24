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
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly ICraftingProcessor _craftingProcessor;
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly RecipeTemplateExpander _templateExpander;

    public CraftItemHandler(
        IIndustryRepository industryRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICraftingProcessor craftingProcessor,
        ICommandDispatcher commandDispatcher,
        RecipeTemplateExpander templateExpander)
    {
        _industryRepository = industryRepository;
        _knowledgeRepository = knowledgeRepository;
        _craftingProcessor = craftingProcessor;
        _commandDispatcher = commandDispatcher;
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

        // Character membership is validated by AwardProficiencyCommand, which loads the
        // membership, awards the XP, and fails with an explicit result if the character is
        // not a member of the industry.

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

        // Build result data — always include the computed products so the caller can grant them
        Dictionary<string, object> resultData = new()
        {
            ["products"] = craftingResult.ProductsCreated
        };

        // Award progression points if successful. Delegated to the dispatch boundary
        // so the award gets logging, the generic CommandExecutedEvent, and a Fail contract.
        if (craftingResult.ProgressionPointsAwarded > 0)
        {
            CommandResult progressionResult = await _commandDispatcher.DispatchAsync(new AwardProgressionCommand
            {
                CharacterId = command.CharacterId,
                Points = craftingResult.ProgressionPointsAwarded
            }, cancellationToken);

            if (progressionResult is { Success: true })
            {
                resultData["knowledgePointsEarned"] = (int)progressionResult.Data!["knowledgePointsEarned"];
                resultData["newTotalKnowledgePoints"] = (int)progressionResult.Data!["newTotalKnowledgePoints"];
                resultData["progressionPointsRemaining"] = (int)progressionResult.Data!["progressionPointsRemaining"];
                resultData["progressionPointsRequired"] = (int)progressionResult.Data!["progressionPointsRequired"];
                resultData["isAtSoftCap"] = (bool)progressionResult.Data!["isAtSoftCap"];
                resultData["isAtHardCap"] = (bool)progressionResult.Data!["isAtHardCap"];
                resultData["message"] = progressionResult.Data!["message"];
            }
        }

        // Award proficiency XP if successful. Delegated to the dispatch boundary
        // so the award gets logging, the generic CommandExecutedEvent, and a Fail contract.
        if (craftingResult.ProficiencyXpAwarded > 0)
        {
            CommandResult proficiencyResult = await _commandDispatcher.DispatchAsync(new AwardProficiencyCommand
            {
                CharacterId = command.CharacterId,
                IndustryTag = command.IndustryTag,
                Points = craftingResult.ProficiencyXpAwarded
            }, cancellationToken);

            if (proficiencyResult is { Success: true })
            {
                resultData["proficiencyXpLevel"] = (int)proficiencyResult.Data!["proficiencyXpLevel"];
                resultData["proficiencyXpRemaining"] = (int)proficiencyResult.Data!["proficiencyXpRemaining"];
                resultData["proficiencyXpRequired"] = (int)proficiencyResult.Data!["proficiencyXpRequired"];
                resultData["proficiencyLevelsGained"] = (int)proficiencyResult.Data!["proficiencyLevelsGained"];
                resultData["proficiencyAtTierCeiling"] = (bool)proficiencyResult.Data!["proficiencyAtTierCeiling"];
                resultData["message"] = proficiencyResult.Data!["message"];
            }
        }

        return CommandResult.Ok(resultData);
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
            ProgressionPointsAwarded = recipe.ProgressionPointsAwarded,
            ProficiencyXpAwarded = recipe.ProficiencyXpAwarded
        };

        return Task.FromResult(result);
    }
}
