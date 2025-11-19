using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;
using System.Linq;

using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionsByCategoryQuery, List<PublicItemDefinition>>))]
public sealed class GetItemDefinitionsByCategoryQueryHandler : IQueryHandler<GetItemDefinitionsByCategoryQuery, List<PublicItemDefinition>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionsByCategoryQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PublicItemDefinition>> HandleAsync(GetItemDefinitionsByCategoryQuery query, CancellationToken cancellationToken = default)
    {
        List<PublicItemDefinition> results = _repository.AllItems()
            .Select(static (InternalItemDefinition d) => ItemDefinitionMapper.Map(d))
            .Where(d => d is not null && d!.Category == query.Category)
            .Select(d => d!)
            .ToList();

        return Task.FromResult(results);
    }
}
