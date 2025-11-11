using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
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

    public CraftItemHandler(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        ICraftingProcessor craftingProcessor)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _craftingProcessor = craftingProcessor;
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

        // Check proficiency level
        if (membership.Level < recipe.RequiredProficiency)
        {
            return CommandResult.Fail(
                $"Insufficient proficiency. Required: {recipe.RequiredProficiency}, Current: {membership.Level}");
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

        // Execute crafting process (industry-specific logic via processor)
        CraftingResult craftingResult =
            await _craftingProcessor.ProcessCraftingAsync(command.CharacterId, recipe, command.Context);

        if (!craftingResult.Success)
        {
            return CommandResult.Fail(craftingResult.Message);
        }

        // Award knowledge points if successful
        if (craftingResult.KnowledgePointsAwarded > 0)
        {
            membership.Level = DetermineLevelAfterCrafting(membership.Level, craftingResult.KnowledgePointsAwarded);
        }

        return CommandResult.Ok();
    }

    private ProficiencyLevel DetermineLevelAfterCrafting(ProficiencyLevel currentLevel, int pointsAwarded)
    {
        // Simple placeholder - could be more sophisticated
        return currentLevel;
    }
}

/// <summary>
/// Interface for industry-specific crafting logic
/// Different industries can implement their own processors
/// </summary>
public interface ICraftingProcessor
{
    Task<CraftingResult> ProcessCraftingAsync(CharacterId characterId, Recipe recipe,
        Dictionary<string, object> context);
}

/// <summary>
/// Default crafting processor for generic crafting
/// </summary>
[ServiceBinding(typeof(ICraftingProcessor))]
public class DefaultCraftingProcessor : ICraftingProcessor
{
    public Task<CraftingResult> ProcessCraftingAsync(CharacterId characterId, Recipe recipe,
        Dictionary<string, object> context)
    {
        // Default implementation - assumes ingredients are available and consumes them
        // Real implementation would check inventory, consume ingredients, create products, etc.

        CraftingResult result = new CraftingResult
        {
            Success = true,
            Message = "Crafting completed successfully",
            ProductsCreated = recipe.Products.ToList(),
            IngredientsConsumed = recipe.Ingredients.ToList(),
            KnowledgePointsAwarded = recipe.KnowledgePointsAwarded
        };

        return Task.FromResult(result);
    }
}
