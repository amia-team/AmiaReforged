using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;
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
    ICommandHandler<HarvestResourceCommand> harvestCommandHandler,
    WindowDirector windowDirector) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Tracks active harvest progress bars per player. One bar at a time per player.
    /// </summary>
    private readonly Dictionary<NwPlayer, HarvestProgressPresenter> _activeProgressBars = new();

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
                    {
                        int current = result.Data?.GetValueOrDefault("currentProgress") is int cp ? cp : 0;
                        int total = result.Data?.GetValueOrDefault("requiredProgress") is int rp ? rp : 1;

                        if (!_activeProgressBars.TryGetValue(player, out HarvestProgressPresenter? presenter))
                        {
                            string nodeName = node.Instance.Definition.Name;
                            HarvestProgressView view = new(player, $"Mining {nodeName}");
                            presenter = view.Presenter;
                            NwPlayer closurePlayer = player;
                            presenter.OnClosed += () => _activeProgressBars.Remove(closurePlayer);
                            windowDirector.OpenWindow(presenter);
                            _activeProgressBars[player] = presenter;
                        }

                        presenter.UpdateProgress(current, total);

                        Effect visualEffect = Effect.VisualEffect(VfxType.ComChunkStoneMedium);
                        plc.Location.ApplyEffect(EffectDuration.Instant, visualEffect);
                        break;
                    }
                    case "Completed":
                    {
                        if (_activeProgressBars.TryGetValue(player, out HarvestProgressPresenter? presenter))
                        {
                            presenter.Complete();
                        }

                        player.FloatingTextString($"This resource has {node.Instance.Uses} uses left.");
                        Effect completeEffect = Effect.VisualEffect(VfxType.ComChunkStoneMedium);
                        plc.Location.ApplyEffect(EffectDuration.Instant, completeEffect);
                        break;
                    }
                    case "NodeDepleted":
                    {
                        if (_activeProgressBars.TryGetValue(player, out HarvestProgressPresenter? presenter))
                        {
                            presenter.Complete();
                        }

                        // Node will be destroyed by event handler
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling mineral harvest");
            }
        });
    }
}
