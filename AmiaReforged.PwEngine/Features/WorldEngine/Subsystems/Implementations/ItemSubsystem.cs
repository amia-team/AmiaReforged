using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Implementation of the Item subsystem backed by CQRS queries and item definition repositories.
/// </summary>
[ServiceBinding(typeof(IItemSubsystem))]
public sealed class ItemSubsystem : IItemSubsystem
{
    private readonly IQueryDispatcher _queries;

    public ItemSubsystem(IQueryDispatcher queries)
    {
        _queries = queries;
    }

    public Task<ItemDefinition?> GetItemDefinitionByTagAsync(string tag, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetItemDefinitionByTagQuery, ItemDefinition?>(new GetItemDefinitionByTagQuery(tag), ct);
    }

    public Task<ItemDefinition?> GetItemDefinitionAsync(string resref, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetItemDefinitionByResrefQuery, ItemDefinition?>(new GetItemDefinitionByResrefQuery(resref), ct);
    }

    public Task<List<ItemDefinition>> GetAllItemDefinitionsAsync(CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetAllItemDefinitionsQuery, List<ItemDefinition>>(new GetAllItemDefinitionsQuery(), ct);
    }

    public Task<List<ItemDefinition>> SearchItemDefinitionsAsync(string searchTerm, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<SearchItemDefinitionsQuery, List<ItemDefinition>>(new SearchItemDefinitionsQuery(searchTerm), ct);
    }

    public Task<Dictionary<string, object>> GetItemPropertiesAsync(string resref, CancellationToken ct = default)
    {
        // For now, return empty properties; these can be backed by a dedicated repository/command later.
        return Task.FromResult(new Dictionary<string, object>());
    }

    public Task<CommandResult> UpdateItemPropertiesAsync(string resref, Dictionary<string, object> properties, CancellationToken ct = default)
    {
        // Blueprint mutation is not yet supported; expose as a failing command result.
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<List<ItemDefinition>> GetItemsByCategoryAsync(ItemCategory category, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetItemDefinitionsByCategoryQuery, List<ItemDefinition>>(new GetItemDefinitionsByCategoryQuery(category), ct);
    }

    public Task<List<ItemDefinition>> GetItemsByTagsAsync(List<string> tags, CancellationToken ct = default)
    {
        return _queries.DispatchAsync<GetItemDefinitionsByTagsQuery, List<ItemDefinition>>(new GetItemDefinitionsByTagsQuery(tags), ct);
    }
}
