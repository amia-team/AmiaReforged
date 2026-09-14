using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
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
/// Ingress adapter for Flora-type nodes (plants, herbs, mushrooms, etc.):
/// translates the player-use event into the character's "harvesting"
/// interaction session. All domain logic — tool checks, outputs, depletion —
/// is owned by the interaction framework. This class owns only NWN event
/// wiring and gather UI.
/// </summary>
[ServiceBinding(typeof(INodeHarvestStrategy))]
public sealed class FloraGatherStrategy(
    RuntimeCharacterService characterService,
    Lazy<RuntimeNodeService> runtimeNodeService,
    ICommandDispatcher commandDispatcher,
    WindowDirector windowDirector) : INodeHarvestStrategy
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly HashSet<ResourceType> Types = new() { ResourceType.Flora };

    public IReadOnlySet<ResourceType> SupportedTypes => Types;

    public void WireEvents(NwPlaceable placeable, SpawnedNode node)
    {
        placeable.OnUsed += HandleGather;
    }

    public void UnwireEvents(NwPlaceable placeable)
    {
        placeable.OnUsed -= HandleGather;
    }

    private void HandleGather(PlaceableEvents.OnUsed obj)
    {
        NwPlaceable plc = obj.Placeable;
        if (!plc.IsValid) return;
        if (obj.UsedBy is null) return;
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        RuntimeCharacter? character = characterService.GetRuntimeCharacter(obj.UsedBy);
        if (character is null) return;

        SpawnedNode? spawnedNode = runtimeNodeService.Value.GetSpawnedNode(plc.UUID);
        if (spawnedNode is null) return;

        ResourceNodeInstance node = spawnedNode.Instance;

        // A gather drives the character's "harvesting" session; flora definitions
        // complete immediately and deplete on first completion.
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
                    player.FloatingTextString(result.ErrorMessage ?? "Gathering failed");
                    return;
                }

                // Show gathering VFX
                Effect gatherEffect = Effect.VisualEffect(VfxType.ImpHeadNature);
                plc.Location.ApplyEffect(EffectDuration.Instant, gatherEffect);

                // Show instant-complete progress bar
                HarvestProgressView view = new(player, $"Gathering {node.Definition.Name}");
                HarvestProgressPresenter presenter = view.Presenter;
                windowDirector.OpenWindow(presenter);
                presenter.UpdateProgress(1, 1);
                presenter.Complete();

                if (result.Data?.GetValueOrDefault("items") is List<HarvestedItem> harvested
                    && harvested.Count > 0)
                {
                    string summary = string.Join(", ",
                        harvested.Select(h => $"{h.Quantity}x {h.ItemTag}"));
                    player.FloatingTextString($"Gathered: {summary}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error gathering flora");
            }
        });
    }
}
