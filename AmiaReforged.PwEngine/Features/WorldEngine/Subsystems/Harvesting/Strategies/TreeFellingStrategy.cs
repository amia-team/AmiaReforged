using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;

/// <summary>
/// Ingress adapter for Tree-type nodes: translates the attack-based pattern
/// (player physically attacks the placeable) into ticks of the character's
/// "harvesting" interaction session. All domain logic — tool checks, progress,
/// log yield (<see cref="TreeProperties"/>), depletion — is owned by the
/// interaction framework. This class owns only NWN event wiring and harvest UI.
/// </summary>
[ServiceBinding(typeof(INodeHarvestStrategy))]
public sealed class TreeFellingStrategy(
    RuntimeCharacterService characterService,
    Lazy<RuntimeNodeService> runtimeNodeService,
    ICommandDispatcher commandDispatcher,
    WindowDirector windowDirector) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Tracks active harvest progress bars per player. One bar at a time per player.
    /// </summary>
    private readonly Dictionary<NwPlayer, HarvestProgressPresenter> _activeProgressBars = new();

    private static readonly HashSet<ResourceType> Types = new() { ResourceType.Tree };

    public IReadOnlySet<ResourceType> SupportedTypes => Types;

    public void WireEvents(NwPlaceable placeable, SpawnedNode node)
    {
        placeable.OnPhysicalAttacked += HandleChop;
    }

    public void UnwireEvents(NwPlaceable placeable)
    {
        placeable.OnPhysicalAttacked -= HandleChop;
    }

    private void HandleChop(PlaceableEvents.OnPhysicalAttacked obj)
    {
        NwPlaceable plc = obj.Placeable;
        if (!plc.IsValid) return;
        if (obj.Attacker is null) return;
        if (!obj.Attacker.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.Attacker);
        if (character is null) return;

        SpawnedNode? spawnedNode = runtimeNodeService.Value.GetSpawnedNode(plc.UUID);
        if (spawnedNode is null) return;

        ResourceNodeInstance node = spawnedNode.Instance;

        // Each chop drives one tick of the character's "harvesting" session.
        PerformInteractionCommand command = new(character.GetId(), "harvesting", node.Id);

        _ = NwTask.Run(async () =>
        {
            try
            {
                await NwTask.SwitchToMainThread();

                CommandResult result = await commandDispatcher.DispatchAsync(command);

                await NwTask.SwitchToMainThread();

                if (!result.Success)
                {
                    player.FloatingTextString(result.ErrorMessage ?? "Chopping failed");
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
                            string nodeName = node.Definition.Name;
                            HarvestProgressView view = new(player, $"Chopping {nodeName}");
                            presenter = view.Presenter;
                            NwPlayer closurePlayer = player;
                            presenter.OnClosed += () => _activeProgressBars.Remove(closurePlayer);
                            windowDirector.OpenWindow(presenter);
                            _activeProgressBars[player] = presenter;
                        }

                        presenter.UpdateProgress(current, total);

                        Effect dustEffect = Effect.VisualEffect(VfxType.ImpDustExplosion, false, 0.4f);
                        plc.Location.ApplyEffect(EffectDuration.Instant, dustEffect);
                        break;
                    }
                    case "Completed":
                    case "NodeDepleted":
                    {
                        if (_activeProgressBars.TryGetValue(player, out HarvestProgressPresenter? presenter))
                        {
                            presenter.Complete();
                        }

                        if (result.Data?.GetValueOrDefault("items") is List<HarvestedItem> harvested
                            && harvested.Count > 0)
                        {
                            string summary = string.Join(", ",
                                harvested.Select(h => $"{h.Quantity}x {h.ItemTag}"));
                            player.FloatingTextString($"Timber! Harvested: {summary}");
                        }

                        if (status == "NodeDepleted")
                        {
                            node.Uses = 0;
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling tree chop");
            }
        });
    }
}
