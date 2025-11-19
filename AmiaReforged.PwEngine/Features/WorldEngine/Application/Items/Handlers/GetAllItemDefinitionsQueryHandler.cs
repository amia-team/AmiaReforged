using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;
using System.Linq;
using ItemBlueprint = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemBlueprint;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetAllItemDefinitionsQuery, List<ItemBlueprint>>))]
public sealed class GetAllItemDefinitionsQueryHandler : IQueryHandler<GetAllItemDefinitionsQuery, List<ItemBlueprint>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetAllItemDefinitionsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<ItemBlueprint>> HandleAsync(GetAllItemDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        List<ItemBlueprint> results = _repository.AllItems();
        return Task.FromResult(results);
    }
}
