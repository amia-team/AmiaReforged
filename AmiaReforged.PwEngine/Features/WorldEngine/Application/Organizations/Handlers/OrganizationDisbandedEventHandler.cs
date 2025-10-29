using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Handlers;

/// <summary>
/// Handles OrganizationDisbandedEvent by removing all members from the disbanded organization.
/// This ensures proper cleanup when organizations are dissolved.
/// </summary>
[ServiceBinding(typeof(IEventHandler<OrganizationDisbandedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]
public class OrganizationDisbandedEventHandler(
    IOrganizationMemberRepository memberRepository)
    : IEventHandler<OrganizationDisbandedEvent>, IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task HandleAsync(OrganizationDisbandedEvent @event, CancellationToken cancellationToken = default)
    {
        // No need to switch to main thread - this is pure data access
        await Task.CompletedTask;

        Log.Info($"Handling organization disbanded: {nameof(@event.OrganizationId)}={@event.OrganizationId}, Name={@event.Name}");

        // Remove all members from the disbanded organization
        List<OrganizationMember> members = memberRepository.GetByOrganization(@event.OrganizationId);

        if (members.Count == 0)
        {
            Log.Debug($"No members found for disbanded organization {@event.OrganizationId}");
            return;
        }

        Log.Info($"Removing {members.Count} members from disbanded organization {@event.Name}");

        foreach (OrganizationMember member in members)
        {
            memberRepository.Remove(member);
        }

        memberRepository.SaveChanges();

        Log.Info($"Successfully removed all members from disbanded organization {@event.Name}");
    }
}

