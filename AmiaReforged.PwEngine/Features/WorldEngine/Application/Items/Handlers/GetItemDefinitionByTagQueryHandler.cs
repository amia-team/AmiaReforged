using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;

using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionByTagQuery, PublicItemDefinition?>))]
public sealed class GetItemDefinitionByTagQueryHandler : IQueryHandler<GetItemDefinitionByTagQuery, PublicItemDefinition?>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionByTagQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicItemDefinition?> HandleAsync(GetItemDefinitionByTagQuery query, CancellationToken cancellationToken = default)
    {
        InternalItemDefinition? internalDef = _repository.GetByTag(query.Tag);
        PublicItemDefinition? result = ItemDefinitionMapper.Map(internalDef);
        return Task.FromResult(result);
    }
}
