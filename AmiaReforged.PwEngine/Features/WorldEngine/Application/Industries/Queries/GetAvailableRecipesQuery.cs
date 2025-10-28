using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Query to get recipes a character can craft (has knowledge + proficiency)
/// </summary>
public record GetAvailableRecipesQuery : IQuery<List<Recipe>>
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
}

/// <summary>
/// Handles retrieving available recipes for a character
/// </summary>
public class GetAvailableRecipesHandler : IQueryHandler<GetAvailableRecipesQuery, List<Recipe>>
{
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;

    public GetAvailableRecipesHandler(
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository)
    {
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
    }

    public Task<List<Recipe>> HandleAsync(GetAvailableRecipesQuery query, CancellationToken cancellationToken = default)
    {
        Industry? industry = _industryRepository.GetByTag(query.IndustryTag);
        if (industry == null)
        {
            return Task.FromResult(new List<Recipe>());
        }

        List<IndustryMembership> memberships = _membershipRepository.All(query.CharacterId.Value);
        IndustryMembership? membership = memberships.FirstOrDefault(m => m.IndustryTag.Value == query.IndustryTag.Value);
        if (membership == null)
        {
            return Task.FromResult(new List<Recipe>());
        }

        List<Knowledge> characterKnowledge = _knowledgeRepository.GetAllKnowledge(query.CharacterId.Value);
        HashSet<string> knownTags = characterKnowledge.Select(ck => ck.Tag).ToHashSet();

        List<Recipe> availableRecipes = industry.Recipes
            .Where(recipe =>
                // Character has sufficient proficiency
                membership.Level >= recipe.RequiredProficiency &&
                // Character has all required knowledge
                recipe.RequiredKnowledge.All(req => knownTags.Contains(req)))
            .ToList();

        return Task.FromResult(availableRecipes);
    }
}

