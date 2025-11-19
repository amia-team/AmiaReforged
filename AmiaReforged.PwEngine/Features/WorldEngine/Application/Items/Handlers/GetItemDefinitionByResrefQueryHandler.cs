using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;


namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionByResrefQuery, ItemBlueprint?>))]
public sealed class GetItemDefinitionByResrefQueryHandler : IQueryHandler<GetItemDefinitionByResrefQuery, ItemBlueprint?>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionByResrefQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<ItemBlueprint?> HandleAsync(GetItemDefinitionByResrefQuery query, CancellationToken cancellationToken = default)
    {
        ItemBlueprint? match = _repository.AllItems()
            .FirstOrDefault(d => string.Equals(d.ResRef, query.Resref, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match);
    }
}
