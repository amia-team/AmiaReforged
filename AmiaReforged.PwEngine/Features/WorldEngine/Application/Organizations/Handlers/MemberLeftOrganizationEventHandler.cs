using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Handlers;

/// <summary>
/// Handles MemberLeftOrganizationEvent to perform cleanup when a member leaves.
/// Currently logs the event - can be extended for permission revocation, notifications, etc.
/// </summary>
[ServiceBinding(typeof(IEventHandler<MemberLeftOrganizationEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class MemberLeftOrganizationEventHandler
    : IEventHandler<MemberLeftOrganizationEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(MemberLeftOrganizationEvent @event, CancellationToken cancellationToken = default)
    {
        // No NWN calls needed currently
        await Task.CompletedTask;

        Log.Info($"Member left organization: MemberId={@event.MemberId.Value}, OrganizationId={@event.OrganizationId.Value}");

        // Future enhancements:
        // - Revoke organization-specific permissions
        // - Clear faction reputation bonuses
        // - Notify organization leaders
        // - Update UI displays
        // - Log to audit trail
    }
}

