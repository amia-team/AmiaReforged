using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;

/// <summary>
/// Handles NodeDepletedEvent by destroying the visual placeable in the game world.
/// This handler switches to the main thread to safely make NWN game calls.
/// </summary>
[ServiceBinding(typeof(IEventHandler<NodeDepletedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class NodeDepletedEventHandler : IEventHandler<NodeDepletedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(NodeDepletedEvent @event, CancellationToken cancellationToken = default)
    {
        // Switch to main thread to safely make NWN calls
        await NwTask.SwitchToMainThread();

        NodeDepletedEvent evt = @event;
        Log.Info($"Node {evt.NodeInstanceId} in {evt.AreaResRef} has been depleted");

        // Optional: Could send a message to DMs
        NwModule.Instance.SendMessageToAllDMs(
            $"Resource node '{evt.ResourceTag}' depleted in {evt.AreaResRef}");
    }
}

