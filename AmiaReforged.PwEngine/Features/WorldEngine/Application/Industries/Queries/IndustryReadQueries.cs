using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Returns the knowledge definitions a character has learned, across all industries.
/// </summary>
public record GetKnowledgeDefinitionsQuery : IQuery<List<Knowledge>>
{
    public required CharacterId CharacterId { get; init; }
}

/// <summary>
/// Returns whether a character is currently eligible to learn a piece of knowledge.
/// </summary>
public record CanLearnKnowledgeQuery : IQuery<bool>
{
    public required CharacterId CharacterId { get; init; }
    public required string KnowledgeTag { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetKnowledgeDefinitionsQuery, List<Knowledge>>))]
public class GetKnowledgeDefinitionsHandler : IQueryHandler<GetKnowledgeDefinitionsQuery, List<Knowledge>>
{
    private readonly ICharacterKnowledgeRepository _knowledgeRepository;

    public GetKnowledgeDefinitionsHandler(ICharacterKnowledgeRepository knowledgeRepository)
    {
        _knowledgeRepository = knowledgeRepository;
    }

    public Task<List<Knowledge>> HandleAsync(GetKnowledgeDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        List<Knowledge> knowledge = _knowledgeRepository.GetAllKnowledge(query.CharacterId.Value);
        return Task.FromResult(knowledge);
    }
}

[ServiceBinding(typeof(IQueryHandler<CanLearnKnowledgeQuery, bool>))]
public class CanLearnKnowledgeHandler : IQueryHandler<CanLearnKnowledgeQuery, bool>
{
    private readonly IIndustryMembershipService _membershipService;

    public CanLearnKnowledgeHandler(IIndustryMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    public Task<bool> HandleAsync(CanLearnKnowledgeQuery query, CancellationToken cancellationToken = default)
    {
        // Learning eligibility mixes rank, knowledge-point and already-known checks; keep that
        // domain logic in the service and expose only the boolean projection through the query.
        bool canLearn = _membershipService.CanLearnKnowledge(query.CharacterId.Value, query.KnowledgeTag);
        return Task.FromResult(canLearn);
    }
}
