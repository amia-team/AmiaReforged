using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;

/// <summary>
/// Stub implementation of the Item subsystem.
/// TODO: Wire up to existing item definition systems.
/// </summary>
[ServiceBinding(typeof(IItemSubsystem))]
public sealed class ItemSubsystem : IItemSubsystem
{
    public Task<ItemDefinition?> GetItemDefinitionAsync(string resref, CancellationToken ct = default)
    {
        return Task.FromResult<ItemDefinition?>(null);
    }

    public Task<List<ItemDefinition>> GetAllItemDefinitionsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<ItemDefinition>());
    }

    public Task<List<ItemDefinition>> SearchItemDefinitionsAsync(string searchTerm, CancellationToken ct = default)
    {
        return Task.FromResult(new List<ItemDefinition>());
    }

    public Task<Dictionary<string, object>> GetItemPropertiesAsync(string resref, CancellationToken ct = default)
    {
        return Task.FromResult(new Dictionary<string, object>());
    }

    public Task<CommandResult> UpdateItemPropertiesAsync(string resref, Dictionary<string, object> properties, CancellationToken ct = default)
    {
        return Task.FromResult(CommandResult.Fail("Not yet implemented"));
    }

    public Task<List<ItemDefinition>> GetItemsByCategoryAsync(ItemCategory category, CancellationToken ct = default)
    {
        return Task.FromResult(new List<ItemDefinition>());
    }

    public Task<List<ItemDefinition>> GetItemsByTagsAsync(List<string> tags, CancellationToken ct = default)
    {
        return Task.FromResult(new List<ItemDefinition>());
    }
}

