using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.ResourceNodes.Queries;

/// <summary>
/// Gets a single resource node definition by tag.
/// </summary>
public record GetResourceNodeDefinitionQuery : IQuery<ResourceNodeDefinition?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches resource node definitions by name/tag with an optional type filter.
/// Empty term returns all.
/// </summary>
public record SearchResourceNodeDefinitionsQuery : IQuery<List<ResourceNodeDefinition>>
{
    public string? SearchTerm { get; init; }
    public string? TypeFilter { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetResourceNodeDefinitionQuery, ResourceNodeDefinition?>))]
public sealed class GetResourceNodeDefinitionHandler : IQueryHandler<GetResourceNodeDefinitionQuery, ResourceNodeDefinition?>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public GetResourceNodeDefinitionHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<ResourceNodeDefinition?> HandleAsync(GetResourceNodeDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.Get(query.Tag));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchResourceNodeDefinitionsQuery, List<ResourceNodeDefinition>>))]
public sealed class SearchResourceNodeDefinitionsHandler : IQueryHandler<SearchResourceNodeDefinitionsQuery, List<ResourceNodeDefinition>>
{
    private readonly IResourceNodeDefinitionRepository _repository;

    public SearchResourceNodeDefinitionsHandler(IResourceNodeDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ResourceNodeDefinition>> HandleAsync(SearchResourceNodeDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<ResourceNodeDefinition> all = _repository.All();

        if (!string.IsNullOrWhiteSpace(query.TypeFilter)
            && Enum.TryParse<ResourceType>(query.TypeFilter, true, out ResourceType type))
        {
            all = all.Where(n => n.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim();
            all = all.Where(n => (n.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                                 || (n.Tag?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Task.FromResult(all.ToList());
    }
}
