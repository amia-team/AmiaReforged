using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Handlers;

/// <summary>
/// Handles RegionRemovedEvent to log region removal.
/// Resource nodes don't currently track regions, so cleanup must be handled elsewhere.
/// </summary>
[ServiceBinding(typeof(IEventHandler<RegionRemovedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class RegionRemovedEventHandler
    : IEventHandler<RegionRemovedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(RegionRemovedEvent @event, CancellationToken cancellationToken = default)
    {
        // No NWN calls or cleanup needed currently
        await Task.CompletedTask;

        Log.Info($"Region removed: RegionTag={@event.Tag.Value}");

        // Future enhancements (when region tracking added to resource nodes):
        // - Clear all resource nodes in the removed region
        // - Update area configurations
        // - Notify DMs of region removal
        // - Archive region data for audit trail
    }
}
