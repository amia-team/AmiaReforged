using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;

/// <summary>
/// Gets a single region definition by tag (case-insensitive).
/// </summary>
public record GetRegionDefinitionQuery : IQuery<RegionDefinition?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches region definitions by name/tag. Empty term returns all.
/// </summary>
public record SearchRegionDefinitionsQuery : IQuery<List<RegionDefinition>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetRegionDefinitionQuery, RegionDefinition?>))]
public sealed class GetRegionDefinitionHandler : IQueryHandler<GetRegionDefinitionQuery, RegionDefinition?>
{
    private readonly IRegionRepository _repository;

    public GetRegionDefinitionHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public Task<RegionDefinition?> HandleAsync(GetRegionDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.All().FirstOrDefault(r =>
            r.Tag.Value.Equals(query.Tag, StringComparison.OrdinalIgnoreCase)));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchRegionDefinitionsQuery, List<RegionDefinition>>))]
public sealed class SearchRegionDefinitionsHandler : IQueryHandler<SearchRegionDefinitionsQuery, List<RegionDefinition>>
{
    private readonly IRegionRepository _repository;

    public SearchRegionDefinitionsHandler(IRegionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<RegionDefinition>> HandleAsync(SearchRegionDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<RegionDefinition> all = _repository.All();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim();
            all = all.Where(r => r.Tag.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
                                 || r.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult(all.ToList());
    }
}
