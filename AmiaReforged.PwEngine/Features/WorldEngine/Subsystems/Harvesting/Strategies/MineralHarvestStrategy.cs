using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;

/// <summary>
/// Harvest strategy for mineral-type nodes (Ore, Geode, Boulder).
/// Uses the attack-based pattern: player physically attacks the placeable,
/// progress accumulates per hit, outputs are granted when complete.
/// Supports multiple harvest cycles via the Uses counter.
/// </summary>
[ServiceBinding(typeof(INodeHarvestStrategy))]
public sealed class MineralHarvestStrategy(
    RuntimeCharacterService characterService,
    Lazy<RuntimeNodeService> runtimeNodeService,
    ICommandHandler<HarvestResourceCommand> harvestCommandHandler) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly HashSet<ResourceType> Types = new()
    {
        ResourceType.Ore,
        ResourceType.Geode,
        ResourceType.Boulder
    };

    public IReadOnlySet<ResourceType> SupportedTypes => Types;

    public void WireEvents(NwPlaceable placeable, SpawnedNode node)
    {
        placeable.OnPhysicalAttacked += HandleAttackedHarvest;
    }

    public void UnwireEvents(NwPlaceable placeable)
    {
        placeable.OnPhysicalAttacked -= HandleAttackedHarvest;
    }

    private void HandleAttackedHarvest(PlaceableEvents.OnPhysicalAttacked obj)
    {
        NwPlaceable plc = obj.Placeable;
        if (!plc.IsValid) return;
        if (obj.Attacker is null) return;
        if (!obj.Attacker.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.Attacker);
        if (character is null) return;

        SpawnedNode? node = runtimeNodeService.Value.GetSpawnedNode(plc.UUID);
        if (node is null) return;

        HarvestResourceCommand command = new(character.GetId().Value, node.Instance.Id);

        _ = NwTask.Run(async () =>
        {
            try
            {
                await NwTask.SwitchToMainThread();

                CommandResult result = await harvestCommandHandler.HandleAsync(command);

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
                        Effect visualEffect = Effect.VisualEffect(VfxType.ComChunkStoneMedium);
                        plc.Location.ApplyEffect(EffectDuration.Instant, visualEffect);
                        break;
                    case "Completed":
                        player.FloatingTextString($"This resource has {node.Instance.Uses} uses left.");
                        Effect completeEffect = Effect.VisualEffect(VfxType.ComChunkStoneMedium);
                        plc.Location.ApplyEffect(EffectDuration.Instant, completeEffect);
                        break;
                    case "NodeDepleted":
                        // Node will be destroyed by event handler
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling mineral harvest");
            }
        });
    }
}
