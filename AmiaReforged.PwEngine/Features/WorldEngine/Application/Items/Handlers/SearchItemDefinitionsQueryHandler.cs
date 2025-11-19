using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<SearchItemDefinitionsQuery, List<ItemBlueprint>>))]
public sealed class SearchItemDefinitionsQueryHandler : IQueryHandler<SearchItemDefinitionsQuery, List<ItemBlueprint>>
{
    private readonly IItemDefinitionRepository _repository;

    public SearchItemDefinitionsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ItemBlueprint>> HandleAsync(SearchItemDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            return Task.FromResult(new List<ItemBlueprint>());
        }

        string term = query.SearchTerm.Trim();

        List<ItemBlueprint> results = _repository.AllItems()
            .Where(d => d.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || d.ItemTag.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(results);
    }
}
