using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using Anvil.Services;
using System.Linq;

using InternalItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData.ItemDefinition;
using PublicItemDefinition = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ItemDefinition;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Handlers;

[ServiceBinding(typeof(IQueryHandler<GetItemDefinitionsByTagsQuery, List<PublicItemDefinition>>))]
public sealed class GetItemDefinitionsByTagsQueryHandler : IQueryHandler<GetItemDefinitionsByTagsQuery, List<PublicItemDefinition>>
{
    private readonly IItemDefinitionRepository _repository;

    public GetItemDefinitionsByTagsQueryHandler(IItemDefinitionRepository repository)
    {
        _repository = repository;
    }

    public Task<List<PublicItemDefinition>> HandleAsync(GetItemDefinitionsByTagsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Tags.Count == 0)
        {
            return Task.FromResult(new List<PublicItemDefinition>());
        }

        HashSet<string> tagSet = new(query.Tags, StringComparer.OrdinalIgnoreCase);

        List<PublicItemDefinition> results = _repository.AllItems()
            .Where(d => tagSet.Contains(d.ItemTag))
            .Select(static (InternalItemDefinition d) => ItemDefinitionMapper.Map(d))
            .Where(d => d is not null)
            .Select(d => d!)
            .ToList();

        return Task.FromResult(results);
    }
}
