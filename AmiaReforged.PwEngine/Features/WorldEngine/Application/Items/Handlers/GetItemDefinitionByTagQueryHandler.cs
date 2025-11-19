using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;


namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionByTagQuery, ItemBlueprint?>))]
public sealed class GetItemDefinitionByTagQueryHandler : IQueryHandler<GetItemDefinitionByTagQuery, ItemBlueprint?>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionByTagQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<ItemBlueprint?> HandleAsync(GetItemDefinitionByTagQuery query, CancellationToken cancellationToken = default)
    {
        ItemBlueprint? internalDef = _repository.GetByTag(query.Tag);
        return Task.FromResult(internalDef);
    }
}
