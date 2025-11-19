using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;

using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionByResrefQuery, PublicItemDefinition?>))]
public sealed class GetItemDefinitionByResrefQueryHandler : IQueryHandler<GetItemDefinitionByResrefQuery, PublicItemDefinition?>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionByResrefQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<PublicItemDefinition?> HandleAsync(GetItemDefinitionByResrefQuery query, CancellationToken cancellationToken = default)
    {
        InternalItemDefinition? match = _repository.AllItems()
            .FirstOrDefault(d => string.Equals(d.ResRef, query.Resref, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(ItemDefinitionMapper.Map(match));
    }
}
