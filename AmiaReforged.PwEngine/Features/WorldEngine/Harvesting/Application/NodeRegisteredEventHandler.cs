using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

/// <summary>
/// Handles NodeRegisteredEvent by creating the visual placeable in the game world.
/// This handler switches to the main thread to safely make NWN game calls.
/// </summary>
[ServiceBinding(typeof(IEventHandler<NodeRegisteredEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class NodeRegisteredEventHandler : IEventHandler<NodeRegisteredEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(NodeRegisteredEvent @event, CancellationToken cancellationToken = default)
    {
        // Switch to main thread to safely make NWN calls
        await NwTask.SwitchToMainThread();

        NodeRegisteredEvent evt = @event;
        Log.Info($"Node {evt.NodeInstanceId} registered in {evt.AreaResRef}: {evt.ResourceTag} (Quality: {evt.Quality}, Uses: {evt.InitialUses})");

        // Optional: Send message to DMs
        NwArea? area = NwModule.Instance.Areas
            .FirstOrDefault(a => a.ResRef.ToString() == evt.AreaResRef);
        if (area != null)
        {
            NwModule.Instance.SendMessageToAllDMs(
                $"New resource node registered in {area.Name}: {evt.ResourceTag} (Q: {evt.Quality})");
        }
    }
}

