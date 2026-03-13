using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Query to get all recipes a character can craft at a specific workstation.
/// Resolves the workstation's supported industries, intersects with the character's
/// industry memberships, and filters recipes by proficiency and knowledge requirements.
/// </summary>
public record GetWorkstationRecipesQuery : IQuery<List<Recipe>>
{
    public required CharacterId CharacterId { get; init; }
    public required WorkstationTag WorkstationTag { get; init; }
}

/// <summary>
/// Handles retrieving available recipes for a character at a given workstation.
/// Includes both manually-defined recipes and template-expanded recipes.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetWorkstationRecipesQuery, List<Recipe>>))]
public class GetWorkstationRecipesHandler : IQueryHandler<GetWorkstationRecipesQuery, List<Recipe>>
{
    private readonly IWorkstationRepository _workstationRepository;
    private readonly IIndustryRepository _industryRepository;
    private readonly IIndustryMembershipRepository _membershipRepository;
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;
    private readonly RecipeTemplateExpander _templateExpander;

    public GetWorkstationRecipesHandler(
        IWorkstationRepository workstationRepository,
        IIndustryRepository industryRepository,
        IIndustryMembershipRepository membershipRepository,
        ICharacterKnowledgeRepository knowledgeRepository,
        RecipeTemplateExpander templateExpander)
    {
        _workstationRepository = workstationRepository;
        _industryRepository = industryRepository;
        _membershipRepository = membershipRepository;
        _knowledgeRepository = knowledgeRepository;
        _templateExpander = templateExpander;
    }

    public Task<List<Recipe>> HandleAsync(GetWorkstationRecipesQuery query, CancellationToken cancellationToken = default)
    {
        // 1. Resolve the workstation definition
        Workstation? workstation = _workstationRepository.GetByTag(query.WorkstationTag);
        if (workstation == null)
            return Task.FromResult(new List<Recipe>());

        // 2. Get the character's industry memberships
        List<IndustryMembership> memberships = _membershipRepository.All(query.CharacterId.Value);
        if (memberships.Count == 0)
            return Task.FromResult(new List<Recipe>());

        // 3. Build a set of industry tags the workstation supports
        HashSet<string> supportedIndustryTags = workstation.SupportedIndustries
            .Select(it => it.Value)
            .ToHashSet();

        // 4. Get the character's known knowledge tags (across all industries)
        List<Knowledge> characterKnowledge = _knowledgeRepository.GetAllKnowledge(query.CharacterId.Value);
        HashSet<string> knownTags = characterKnowledge.Select(k => k.Tag).ToHashSet();

        // 5. For each industry the character belongs to AND the workstation supports,
        //    collect recipes that match the workstation, proficiency, and knowledge requirements.
        List<Recipe> availableRecipes = new();

        foreach (IndustryMembership membership in memberships)
        {
            // Skip industries this workstation doesn't support
            if (!supportedIndustryTags.Contains(membership.IndustryTag.Value))
                continue;

            Industry? industry = _industryRepository.GetByTag(membership.IndustryTag);
            if (industry == null) continue;

            IEnumerable<Recipe> matching = industry.Recipes.Where(recipe =>
                // Recipe must require THIS workstation
                recipe.RequiredWorkstation != null &&
                recipe.RequiredWorkstation.Value.Value == query.WorkstationTag.Value &&
                // Character has sufficient proficiency
                membership.Level >= recipe.RequiredProficiency &&
                // Character has all required knowledge
                recipe.RequiredKnowledge.All(req => knownTags.Contains(req)));

            availableRecipes.AddRange(matching);

            // 5b. Also include template-expanded recipes for this industry + workstation
            IEnumerable<Recipe> templateRecipes = _templateExpander
                .GetExpandedRecipesForWorkstation(membership.IndustryTag, query.WorkstationTag)
                .Where(recipe =>
                    membership.Level >= recipe.RequiredProficiency &&
                    recipe.RequiredKnowledge.All(req => knownTags.Contains(req)));

            availableRecipes.AddRange(templateRecipes);
        }

        // Deduplicate by RecipeId (manual recipes take precedence over template-expanded)
        List<Recipe> deduplicated = availableRecipes
            .GroupBy(r => r.RecipeId.Value)
            .Select(g => g.First())
            .ToList();

        return Task.FromResult(deduplicated);
    }
}
