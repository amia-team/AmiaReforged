using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;

[ServiceBinding(typeof(RuntimeNodeService))]
public class RuntimeNodeService(
    RuntimeCharacterService characterService,
    ICommandHandler<HarvestResourceCommand> harvestCommandHandler)
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

        // Execute harvest command
        HarvestResourceCommand command = new HarvestResourceCommand(
            character.GetId().Value,
            node.Instance.Id
        );

        // Fire and forget - command handler will publish events
        _ = NwTask.Run(async () =>
        {
            try
            {
                // Ensure we're on the main thread for NWN VM calls in the command handler
                await NwTask.SwitchToMainThread();
                
                CommandResult result = await harvestCommandHandler.HandleAsync(command);

                // Ensure we're still on main thread for NWN API calls
                await NwTask.SwitchToMainThread();

                if (!result.Success)
                {
                    player.FloatingTextString(result.ErrorMessage ?? "Harvest failed");
                    return;
                }

                string? status = result.Data?.GetValueOrDefault("status") as string;

                switch (status)
                {
                    case "InProgress":

                        Effect visualEffect = node.Instance.Definition.Type == ResourceType.Tree
                            ? Effect.VisualEffect(VfxType.ImpDustExplosion, false, 0.4f)
                            : Effect.VisualEffect(VfxType.ComChunkStoneMedium);

                        plc.Location.ApplyEffect(EffectDuration.Instant, visualEffect);
                        break;
                    case "Completed":
                        player.FloatingTextString($"This resource has {node.Instance.Uses} uses left.");
                        Effect completeEffect = node.Instance.Definition.Type == ResourceType.Tree
                            ? Effect.VisualEffect(VfxType.ImpDustExplosion, false, 0.4f)
                            : Effect.VisualEffect(VfxType.ComChunkStoneMedium);

                        plc.Location.ApplyEffect(EffectDuration.Instant, completeEffect);
                        break;
                    case "NodeDepleted":
                        // Node will be destroyed by event handler
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling harvest");
            }
        });
    }
}
