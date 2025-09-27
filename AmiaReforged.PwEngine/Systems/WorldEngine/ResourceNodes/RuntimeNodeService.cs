using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;

[ServiceBinding(typeof(RuntimeNodeService))]
public class RuntimeNodeService(RuntimeCharacterService characterService, IHarvestProcessor harvestService)
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<Guid, SpawnedNode> _spawnedNodes = new();

    public void RegisterPlaceable(NwPlaceable placeable, ResourceNodeInstance instance)
    {
        _spawnedNodes.Add(placeable.UUID, new SpawnedNode(placeable, instance));

        switch (instance.Definition.Type)
        {
            case ResourceType.Undefined:
                break;
            case ResourceType.Ore:
            case ResourceType.Geode:
            case ResourceType.Boulder:
            case ResourceType.Tree:
                placeable.OnPhysicalAttacked += HandleAttackedHarvest;
                break;
            case ResourceType.Flora:
                break;
        }

        harvestService.RegisterNode(instance);
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
                player.FloatingTextString($"This resource has {node.Instance.Uses} uses left.");
                break;
            case HarvestResult.InProgress:
                Effect visualEffect = node.Instance.Definition.Type == ResourceType.Tree
                    ? Effect.VisualEffect(VfxType.ImpDustExplosion, false, 0.4f)
                    : Effect.VisualEffect(VfxType.ComChunkStoneMedium);

                plc.Location.ApplyEffect(EffectDuration.Instant, visualEffect);
                break;
            case HarvestResult.NoTool:
                player.FloatingTextString("You don't have the correct tool for this job.");
                break;
        }
    }
}
