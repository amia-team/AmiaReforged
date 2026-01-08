using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Handles the RemoveStallMemberCommand.
/// Validates and removes a member from the stall, then publishes a domain event.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<RemoveStallMemberCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class RemoveStallMemberCommandHandler : ICommandHandler<RemoveStallMemberCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly IEventBus _eventBus;

    public RemoveStallMemberCommandHandler(
        IPlayerShopRepository shops,
        IEventBus eventBus)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task<CommandResult> HandleAsync(
        RemoveStallMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PlayerStall? stall = _shops.GetShopWithMembers(command.StallId);
            if (stall is null)
            {
                return CommandResult.Fail($"Stall {command.StallId} not found");
            }

            // Validate using domain aggregate
            PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

            PlayerStallDomainResult<string> domainResult = 
                aggregate.TryRemoveMember(command.RequestorPersonaId, command.MemberPersonaId);

            if (!domainResult.Success)
            {
                return CommandResult.Fail(domainResult.ErrorMessage ?? "Failed to remove member");
            }

            bool removed = _shops.RemoveMember(command.StallId, command.MemberPersonaId);
            if (!removed)
            {
                return CommandResult.Fail($"Failed to remove member from stall {command.StallId}");
            }

            Log.Info("Stall {StallId} removed member: {MemberPersonaId}",
                command.StallId, command.MemberPersonaId);

            // Publish domain event
            StallMemberRemovedEvent evt = new()
            {
                StallId = command.StallId,
                MemberPersonaId = command.MemberPersonaId,
                RemovedByPersonaId = command.RequestorPersonaId
            };

            await _eventBus.PublishAsync(evt, cancellationToken).ConfigureAwait(false);

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove member from stall {StallId}", command.StallId);
            return CommandResult.Fail($"Failed to remove member: {ex.Message}");
        }
    }
}
