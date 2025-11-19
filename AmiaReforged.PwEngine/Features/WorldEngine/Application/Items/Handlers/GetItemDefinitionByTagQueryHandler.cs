using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionByTagQuery, ItemDefinition?>))]
public sealed class GetItemDefinitionByTagQueryHandler : IQueryHandler<GetItemDefinitionByTagQuery, ItemDefinition?>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionByTagQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<ItemDefinition?> HandleAsync(GetItemDefinitionByTagQuery query, CancellationToken cancellationToken = default)
    {
        // Repository works in terms of ItemData.ItemDefinition; map to public ItemDefinition.
        var internalDef = _repository.GetByTag(query.Tag);
        if (internalDef == null)
        {
            return Task.FromResult<ItemDefinition?>(null);
        }

        ItemDefinition result = new(
            internalDef.ResRef,
            internalDef.Name,
            internalDef.Description,
            MapCategory(internalDef.JobSystemType),
            internalDef.BaseValue,
            new Dictionary<string, object>()); // TODO: enrich with materials, tags, etc.

        return Task.FromResult<ItemDefinition?>(result);
    }

    private static ItemCategory MapCategory(JobSystemItemType jobType)
    {
        // Simplified mapping; refine as needed.
        return jobType switch
        {
            JobSystemItemType.ResourceOre => ItemCategory.Resource,
            _ => ItemCategory.Miscellaneous
        };
    }
}

