using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

[ServiceBinding(typeof(RuntimeNodeService))]
public class RuntimeNodeService(RuntimeCharacterService characterService, IHarvestProcessor harvestService)
{
    private readonly Dictionary<Guid, SpawnedNode> _spawnedNodes = new();

    public void RegisterPlaceable(NwPlaceable placeable, ResourceNodeInstance instance)
    {
        _spawnedNodes.Add(placeable.UUID, new SpawnedNode(placeable, instance));

        switch (instance.Definition.Type)
        {
            case ResourceType.Undefined:
                break;
            case ResourceType.Ore:
                placeable.OnPhysicalAttacked += HandleAttackedHarvest;
                break;
            case ResourceType.Geode:
                break;
            case ResourceType.Boulder:
                break;
            case ResourceType.Tree:
                break;
            case ResourceType.Flora:
                break;
        }

        harvestService.RegisterNode(instance);
        NwModule.Instance.SendMessageToAllDMs($"New resource node registered in {placeable.Area?.Name}");

        // We only delete the record of the placeable, not the node itself.
        placeable.OnDeath += Delete;

        instance.OnDestroyed += DestroyPlc;
    }

    private void Delete(PlaceableEvents.OnDeath obj)
    {
        Guid id = obj.KilledObject.UUID;

        _spawnedNodes.Remove(id);
    }

    private void DestroyPlc(ResourceNodeInstance instance)
    {
        NwModule.Instance.SendMessageToAllDMs($"Attempting to destroy a node instance in {instance.Area} . . .");

        SpawnedNode? node = _spawnedNodes.GetValueOrDefault(instance.Id);

        if (node is null) return;

        node.Placeable?.Destroy();

        _spawnedNodes.Remove(node.Instance.Id);
    }

    private void HandleAttackedHarvest(PlaceableEvents.OnPhysicalAttacked obj)
    {
        SpawnedNode? node = _spawnedNodes.GetValueOrDefault(obj.Placeable.UUID);

        if (node is null) return;

        NwPlaceable? plc = node.Placeable;

        if (plc is null || !plc.IsValid) return;

        if (obj.Attacker is null) return;

        if (!obj.Attacker.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.Attacker);

        if (character is null) return;

        HarvestResult result = node.Instance.Harvest(character);

        switch (result)
        {
            case HarvestResult.Finished:
                player.FloatingTextString($"This node has {node.Instance.Uses} uses left.");
                break;
            case HarvestResult.InProgress:
                plc.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ComChunkStoneMedium));
                break;
            case HarvestResult.NoTool:
                player.FloatingTextString("You don't have the correct tool for this job.");
                break;
        }
    }
}
