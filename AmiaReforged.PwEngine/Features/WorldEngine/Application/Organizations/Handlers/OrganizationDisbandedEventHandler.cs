using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Handlers;

[ServiceBinding(typeof(IEventHandler<OrganizationDisbandedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class OrganizationDisbandedEventHandler : IEventHandler<OrganizationDisbandedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public Task HandleAsync(OrganizationDisbandedEvent @event, CancellationToken cancellationToken = default)
    {
        // Log the disbanding for audit trail
        Log.Info($"Organization {(@event.Name ?? @event.OrganizationId.ToString())} has been disbanded");

        // Note: Member cleanup happens in the command handler that disbands the organization
        // This handler is for cross-subsystem reactions (future integrations)
        // Example future uses:
        // - Clear diplomatic relations with other organizations
        // - Remove organization-specific permissions
        // - Clean up organization assets/properties
        // - Send notifications to online members

        return Task.CompletedTask;
    }
}
