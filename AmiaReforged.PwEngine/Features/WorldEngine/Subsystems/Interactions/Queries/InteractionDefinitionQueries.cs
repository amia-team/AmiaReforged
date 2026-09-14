using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Queries;

/// <summary>
/// Gets a single interaction definition by tag.
/// </summary>
public record GetInteractionDefinitionQuery : IQuery<InteractionDefinition?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches interaction definitions by name/tag. Empty term returns all.
/// </summary>
public record SearchInteractionDefinitionsQuery : IQuery<List<InteractionDefinition>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetInteractionDefinitionQuery, InteractionDefinition?>))]
public sealed class GetInteractionDefinitionHandler : IQueryHandler<GetInteractionDefinitionQuery, InteractionDefinition?>
{
    private readonly IInteractionDefinitionRepository _repository;

    public GetInteractionDefinitionHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<InteractionDefinition?> HandleAsync(GetInteractionDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.Get(query.Tag));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchInteractionDefinitionsQuery, List<InteractionDefinition>>))]
public sealed class SearchInteractionDefinitionsHandler : IQueryHandler<SearchInteractionDefinitionsQuery, List<InteractionDefinition>>
{
    private readonly IInteractionDefinitionRepository _repository;

    public SearchInteractionDefinitionsHandler(IInteractionDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<InteractionDefinition>> HandleAsync(SearchInteractionDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
            return Task.FromResult(_repository.All());

        string term = query.SearchTerm.Trim();
        return Task.FromResult(_repository.All()
            .Where(d => d.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || d.Tag.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList());
    }
}
