using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Lists all knowledge cap profiles.
/// </summary>
public record GetCapProfilesQuery : IQuery<List<KnowledgeCapProfile>>;

[ServiceBinding(typeof(IQueryHandler<GetCapProfilesQuery, List<KnowledgeCapProfile>>))]
public sealed class GetCapProfilesHandler : IQueryHandler<GetCapProfilesQuery, List<KnowledgeCapProfile>>
{
    private readonly IKnowledgeCapProfileRepository _repository;

    public GetCapProfilesHandler(IKnowledgeCapProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<List<KnowledgeCapProfile>> HandleAsync(GetCapProfilesQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.GetAll());
    }
}
