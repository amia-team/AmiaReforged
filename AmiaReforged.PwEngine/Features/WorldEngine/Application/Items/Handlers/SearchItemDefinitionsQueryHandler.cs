using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;
using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<SearchItemDefinitionsQuery, List<PublicItemDefinition>>))]
public sealed class SearchItemDefinitionsQueryHandler : IQueryHandler<SearchItemDefinitionsQuery, List<PublicItemDefinition>>
{
    private readonly IItemDefinitionRepository _repository;

    public SearchItemDefinitionsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PublicItemDefinition>> HandleAsync(SearchItemDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            return Task.FromResult(new List<PublicItemDefinition>());
        }

        string term = query.SearchTerm.Trim();

        List<PublicItemDefinition> results = _repository.AllItems()
            .Where(d => d.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                        || d.ItemTag.Contains(term, StringComparison.OrdinalIgnoreCase))
            .Select(static (InternalItemDefinition d) => ItemDefinitionMapper.Map(d))
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        return Task.FromResult(results);
    }
}
