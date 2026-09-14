using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;

/// <summary>
/// Command to teach a recipe to a character.
/// Validation-only by design: recipes carry no learnable state of their own.
/// A recipe becomes usable once its <see cref="Recipe.RequiredKnowledge"/> tags are
/// all known (knowledge itself is learned through <see cref="IIndustryMembershipService"/>.
/// LearnKnowledge, which persists <c>CharacterKnowledge</c> and publishes
/// <c>RecipeLearnedEvent</c>); availability is derived by
/// <c>GetAvailableRecipesQuery</c>. This command centralizes the
/// membership + existence (including template-expanded recipes) + prerequisite
/// checks behind the dispatcher so callers get logging, the Fail contract,
/// and <c>CommandExecutedEvent</c>.
/// </summary>
public record LearnRecipeCommand : ICommand
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
    public required string RecipeId { get; init; }
}

/// <summary>
/// Handles recipe-learning validation.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<LearnRecipeCommand>))]
public class LearnRecipeHandler : ICommandHandler<LearnRecipeCommand>
{
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly RecipeTemplateExpander _templateExpander;

    public LearnRecipeHandler(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        RecipeTemplateExpander templateExpander)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _templateExpander = templateExpander;
    }

    public Task<CommandResult> HandleAsync(LearnRecipeCommand command, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(command.IndustryTag);
        if (industry == null)
        {
            return Task.FromResult(CommandResult.Fail($"Industry '{command.IndustryTag.Value}' not found"));
        }

        List<IndustryMembership> memberships = _membershipRepository.All(command.CharacterId.Value);
        if (memberships.All(m => m.IndustryTag.Value != command.IndustryTag.Value))
        {
            return Task.FromResult(CommandResult.Fail($"Character is not a member of industry '{industry.Name}'"));
        }

        // Manual recipes first; fall back to template-expanded ones (same order as CraftItemHandler).
        Recipe? recipe = industry.Recipes.FirstOrDefault(r => r.RecipeId.Value == command.RecipeId)
            ?? _templateExpander.GetExpandedRecipes(command.IndustryTag)
                .FirstOrDefault(r => r.RecipeId.Value == command.RecipeId);
        if (recipe == null)
        {
            return Task.FromResult(CommandResult.Fail($"Recipe '{command.RecipeId}' not found in industry '{command.IndustryTag.Value}'"));
        }

        // Check if character has the required knowledge prereqs
        List<Knowledge> known = _knowledgeRepository.GetAllKnowledge(command.CharacterId.Value);
        HashSet<string> knownTags = known.Select(k => k.Tag).ToHashSet();
        if (!recipe.RequiredKnowledge.All(req => knownTags.Contains(req)))
        {
            return Task.FromResult(CommandResult.Fail("Missing prerequisite knowledge"));
        }

        return Task.FromResult(CommandResult.Ok());
    }
}
