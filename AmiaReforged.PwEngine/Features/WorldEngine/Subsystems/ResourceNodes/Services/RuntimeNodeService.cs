using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;

[ServiceBinding(typeof(RuntimeNodeService))]
public class RuntimeNodeService(
    NodeHarvestStrategyRegistry strategyRegistry)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<Guid, SpawnedNode> _spawnedNodes = new();

    /// <summary>
    /// Look up a <see cref="SpawnedNode"/> by its placeable UUID.
    /// Used by harvest strategies to resolve the game-world context.
    /// </summary>
    public SpawnedNode? GetSpawnedNode(Guid placeableUuid)
        => _spawnedNodes.GetValueOrDefault(placeableUuid);

    public void RegisterPlaceable(NwPlaceable placeable, ResourceNodeInstance instance)
    {
        SpawnedNode spawnedNode = new(placeable, instance);
        _spawnedNodes.Add(placeable.UUID, spawnedNode);

        // Delegate event wiring to the type-specific harvest strategy
        INodeHarvestStrategy? strategy = strategyRegistry.GetStrategy(instance.Definition.Type);
        strategy?.WireEvents(placeable, spawnedNode);

        NwModule.Instance.SendMessageToAllDMs($"New resource node registered in {placeable.Area?.Name}");
        Log.Info("Node registered.");
        placeable.OnDeath += Delete;
        _spawnedNodes[placeable.UUID].Instance.OnDestroyed += DestroyPlc;
    }

    private void Delete(PlaceableEvents.OnDeath obj)
    {
        Guid id = obj.KilledObject.UUID;

        _spawnedNodes.Remove(id);
    }

    private void DestroyPlc(ResourceNodeInstance instance)
    {
        NwModule.Instance.SendMessageToAllDMs($"Attempting to destroy a node instance in {instance.Area} . . .");
        SpawnedNode node = _spawnedNodes[instance.Id];

        if (node.Placeable is null)
        {
            NwModule.Instance.SendMessageToAllDMs($"plc is null");
        }

        node.Placeable?.Destroy();

        _spawnedNodes.Remove(node.Instance.Id);
    }

    /// <summary>
    /// Destroy all in-game placeables for nodes in the given area and remove them from the runtime dictionary.
    /// Must be called before the DB rows are deleted so the placeables don't become orphaned ghosts.
    /// </summary>
    public void ClearNodesInArea(string areaResRef)
    {
        List<KeyValuePair<Guid, SpawnedNode>> toRemove = _spawnedNodes
            .Where(kvp => kvp.Value.Instance.Area == areaResRef)
            .ToList();

        foreach (KeyValuePair<Guid, SpawnedNode> kvp in toRemove)
        {
            NwPlaceable? plc = kvp.Value.Placeable;
            if (plc is not null && plc.IsValid)
            {
                // Unhook harvest strategy events
                INodeHarvestStrategy? strategy = strategyRegistry.GetStrategy(
                    kvp.Value.Instance.Definition.Type);
                strategy?.UnwireEvents(plc);

                plc.OnDeath -= Delete;
                plc.Destroy();
            }

            _spawnedNodes.Remove(kvp.Key);
        }

        Log.Info($"Cleared {toRemove.Count} runtime node(s) in area {areaResRef}");
    }

    /// <summary>
    /// Returns the number of live resource nodes of the given <paramref name="resourceType"/>
    /// currently tracked in the specified area. Counts ALL nodes (bootstrapped, provisioned, and Glyph-spawned).
    /// </summary>
    public int CountNodesOfTypeInArea(string areaResRef, ResourceType resourceType)
    {
        return _spawnedNodes.Values.Count(n =>
            string.Equals(n.Instance.Area, areaResRef, StringComparison.OrdinalIgnoreCase) &&
            n.Instance.Definition.Type == resourceType);
    }
}
