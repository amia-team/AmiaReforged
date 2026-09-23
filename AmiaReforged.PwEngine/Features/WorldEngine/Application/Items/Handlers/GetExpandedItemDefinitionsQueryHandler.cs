using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetExpandedItemDefinitionsQuery, List<ItemBlueprint>>))]
public sealed class GetExpandedItemDefinitionsQueryHandler : IQueryHandler<GetExpandedItemDefinitionsQuery, List<ItemBlueprint>>
{
    private readonly ItemBlueprintExpander _expander;

    public GetExpandedItemDefinitionsQueryHandler(ItemBlueprintExpander expander)
    {
        _expander = expander;
    }

    public Task<List<ItemBlueprint>> HandleAsync(GetExpandedItemDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        List<ItemBlueprint> expanded = _expander.GetExpandedItemsForTemplate(query.TemplateTag);
        return Task.FromResult(expanded);
    }
}
