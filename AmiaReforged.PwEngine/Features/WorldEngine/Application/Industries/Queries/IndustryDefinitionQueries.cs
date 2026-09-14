using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Gets a single industry definition by tag.
/// </summary>
public record GetIndustryDefinitionQuery : IQuery<Industry?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches industry definitions by name/tag. Empty term returns all.
/// </summary>
public record SearchIndustryDefinitionsQuery : IQuery<List<Industry>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetIndustryDefinitionQuery, Industry?>))]
public sealed class GetIndustryDefinitionHandler : IQueryHandler<GetIndustryDefinitionQuery, Industry?>
{
    private readonly IIndustryRepository _repository;

    public GetIndustryDefinitionHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<Industry?> HandleAsync(GetIndustryDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.Get(query.Tag));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchIndustryDefinitionsQuery, List<Industry>>))]
public sealed class SearchIndustryDefinitionsHandler : IQueryHandler<SearchIndustryDefinitionsQuery, List<Industry>>
{
    private readonly IIndustryRepository _repository;

    public SearchIndustryDefinitionsHandler(IIndustryRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Industry>> HandleAsync(SearchIndustryDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
            return Task.FromResult(_repository.All());

        string term = query.SearchTerm.Trim();
        return Task.FromResult(_repository.All()
            .Where(i => i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || i.Tag.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList());
    }
}
