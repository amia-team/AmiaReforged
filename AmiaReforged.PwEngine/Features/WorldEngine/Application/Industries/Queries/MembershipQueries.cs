using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Query to get a character's membership in a specific industry.
/// </summary>
public record GetMembershipQuery : IQuery<IndustryMembership?>
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
}

/// <summary>
/// Query to get all industries a character is enrolled in.
/// </summary>
public record GetCharacterIndustriesQuery : IQuery<List<IndustryMembership>>
{
    public required CharacterId CharacterId { get; init; }
}

/// <summary>
/// Query to get all recipe (knowledge) tags known by a character in an industry.
/// </summary>
public record GetKnownRecipesQuery : IQuery<List<string>>
{
    public required CharacterId CharacterId { get; init; }
    public required IndustryTag IndustryTag { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetMembershipQuery, IndustryMembership?>))]
public class GetMembershipHandler : IQueryHandler<GetMembershipQuery, IndustryMembership?>
{
    private readonly IIndustryMembershipRepository _membershipRepository;

    public GetMembershipHandler(IIndustryMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
    }

    public Task<IndustryMembership?> HandleAsync(GetMembershipQuery query, CancellationToken cancellationToken = default)
    {
        List<IndustryMembership> memberships = _membershipRepository.All(query.CharacterId.Value);
        IndustryMembership? membership = memberships.FirstOrDefault(m => m.IndustryTag.Value == query.IndustryTag.Value);
        return Task.FromResult(membership);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCharacterIndustriesQuery, List<IndustryMembership>>))]
public class GetCharacterIndustriesHandler : IQueryHandler<GetCharacterIndustriesQuery, List<IndustryMembership>>
{
    private readonly IIndustryMembershipRepository _membershipRepository;

    public GetCharacterIndustriesHandler(IIndustryMembershipRepository membershipRepository)
    {
        _membershipRepository = membershipRepository;
    }

    public Task<List<IndustryMembership>> HandleAsync(GetCharacterIndustriesQuery query, CancellationToken cancellationToken = default)
    {
        List<IndustryMembership> memberships = _membershipRepository.All(query.CharacterId.Value);
        return Task.FromResult(memberships);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetKnownRecipesQuery, List<string>>))]
public class GetKnownRecipesHandler : IQueryHandler<GetKnownRecipesQuery, List<string>>
{
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;

    public GetKnownRecipesHandler(ICharacterKnowledgeRepository knowledgeRepository)
    {
        _knowledgeRepository = knowledgeRepository;
    }

    public Task<List<string>> HandleAsync(GetKnownRecipesQuery query, CancellationToken cancellationToken = default)
    {
        List<CharacterKnowledge> knowledge = _knowledgeRepository.GetKnowledgeForIndustry(query.IndustryTag.Value, query.CharacterId.Value);
        List<string> knownTags = knowledge.Select(k => k.Definition.Tag).ToList();
        return Task.FromResult(knownTags);
    }
}
