using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;

/// <summary>
/// Gets a single workstation definition by tag.
/// </summary>
public record GetWorkstationDefinitionQuery : IQuery<Workstation?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches workstation definitions by name/tag. Empty term returns all.
/// </summary>
public record SearchWorkstationDefinitionsQuery : IQuery<List<Workstation>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetWorkstationDefinitionQuery, Workstation?>))]
public sealed class GetWorkstationDefinitionHandler : IQueryHandler<GetWorkstationDefinitionQuery, Workstation?>
{
    private readonly IWorkstationRepository _repository;

    public GetWorkstationDefinitionHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<Workstation?> HandleAsync(GetWorkstationDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_repository.GetByTag(new WorkstationTag(query.Tag)));
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchWorkstationDefinitionsQuery, List<Workstation>>))]
public sealed class SearchWorkstationDefinitionsHandler : IQueryHandler<SearchWorkstationDefinitionsQuery, List<Workstation>>
{
    private readonly IWorkstationRepository _repository;

    public SearchWorkstationDefinitionsHandler(IWorkstationRepository repository)
    {
        _repository = repository;
    }

    public Task<List<Workstation>> HandleAsync(SearchWorkstationDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
            return Task.FromResult(_repository.All());

        string term = query.SearchTerm.Trim();
        return Task.FromResult(_repository.All()
            .Where(w => w.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || w.Tag.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList());
    }
}
