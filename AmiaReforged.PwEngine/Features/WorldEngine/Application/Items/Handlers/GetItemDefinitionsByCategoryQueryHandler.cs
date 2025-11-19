using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;


namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionsByCategoryQuery, List<ItemBlueprint>>))]
public sealed class GetItemDefinitionsByCategoryQueryHandler : IQueryHandler<GetItemDefinitionsByCategoryQuery, List<ItemBlueprint>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionsByCategoryQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ItemBlueprint>> HandleAsync(GetItemDefinitionsByCategoryQuery query, CancellationToken cancellationToken = default)
    {
        List<ItemBlueprint> results = _repository.AllItems()
            .Where(d => MapCategory(d.JobSystemType) == query.Category)
            .ToList();
        return Task.FromResult(results);
    }

    private static ItemCategory MapCategory(JobSystemItemType jobType) => jobType switch
    {
        JobSystemItemType.ResourceOre => ItemCategory.Resource,
        JobSystemItemType.ResourceStone => ItemCategory.Resource,
        JobSystemItemType.ResourceLog => ItemCategory.Resource,
        JobSystemItemType.ResourcePlank => ItemCategory.Resource,
        JobSystemItemType.ResourceBrick => ItemCategory.Resource,
        JobSystemItemType.ResourceIngot => ItemCategory.Resource,
        JobSystemItemType.ResourceGem => ItemCategory.Resource,
        JobSystemItemType.ResourcePlant => ItemCategory.Resource,
        _ => ItemCategory.Miscellaneous
    };
}
