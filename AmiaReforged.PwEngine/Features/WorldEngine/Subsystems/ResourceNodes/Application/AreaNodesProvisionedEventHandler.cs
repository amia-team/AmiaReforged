using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Events;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Application;

/// <summary>
/// Handles AreaNodesProvisionedEvent by logging and notifying DMs.
/// </summary>
[ServiceBinding(typeof(IEventHandler<AreaNodesProvisionedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class AreaNodesProvisionedEventHandler : IEventHandler<AreaNodesProvisionedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(AreaNodesProvisionedEvent @event, CancellationToken cancellationToken = default)
    {
        await NwTask.SwitchToMainThread();

        Log.Info($"Area '{@event.AreaName}' ({@event.AreaResRef}) provisioned with {@event.NodeCount} resource nodes");

        // Notify DMs
        NwModule.Instance.SendMessageToAllDMs(
            $"[Resource Nodes] Provisioned {@event.NodeCount} nodes in {@event.AreaName}"
        );
    }
}

