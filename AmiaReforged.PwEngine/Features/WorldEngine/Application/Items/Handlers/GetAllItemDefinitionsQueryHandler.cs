using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;
using System.Linq;
using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetAllItemDefinitionsQuery, List<PublicItemDefinition>>))]
public sealed class GetAllItemDefinitionsQueryHandler : IQueryHandler<GetAllItemDefinitionsQuery, List<PublicItemDefinition>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetAllItemDefinitionsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PublicItemDefinition>> HandleAsync(GetAllItemDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        List<PublicItemDefinition> results = _repository.AllItems()
            .Select(static (InternalItemDefinition d) => ItemDefinitionMapper.Map(d))
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        return Task.FromResult(results);
    }
}
