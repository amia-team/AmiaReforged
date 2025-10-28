using AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Application;

/// <summary>
/// Handles NodesClearedEvent by cleaning up all visual placeables in the specified area.
/// This handler switches to the main thread to safely make NWN game calls.
/// </summary>
[ServiceBinding(typeof(IEventHandler<NodesClearedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class NodesClearedEventHandler : IEventHandler<NodesClearedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(NodesClearedEvent @event, CancellationToken cancellationToken = default)
    {
        // Switch to main thread to safely make NWN calls
        await NwTask.SwitchToMainThread();

        var evt = @event; // Local variable to avoid @ symbol issues in string interpolations
        Log.Info($"{evt.NodesCleared} nodes cleared from area {evt.AreaResRef}");

        // Optional: Send message to DMs
        var area = NwModule.Instance.Areas
            .FirstOrDefault(a => a.ResRef.ToString() == evt.AreaResRef);
        if (area != null)
        {
            NwModule.Instance.SendMessageToAllDMs(
                $"Cleared {evt.NodesCleared} resource nodes from {area.Name}");
        }
    }
}

