using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;


namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionsByTagsQuery, List<ItemBlueprint>>))]
public sealed class GetItemDefinitionsByTagsQueryHandler : IQueryHandler<GetItemDefinitionsByTagsQuery, List<ItemBlueprint>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionsByTagsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ItemBlueprint>> HandleAsync(GetItemDefinitionsByTagsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Tags.Count == 0)
        {
            return Task.FromResult(new List<ItemBlueprint>());
        }

        HashSet<string> tagSet = new(query.Tags, StringComparer.OrdinalIgnoreCase);

        List<ItemBlueprint> results = _repository.AllItems()
            .Where(d => tagSet.Contains(d.ItemTag))
            .ToList();

        return Task.FromResult(results);
    }
}
