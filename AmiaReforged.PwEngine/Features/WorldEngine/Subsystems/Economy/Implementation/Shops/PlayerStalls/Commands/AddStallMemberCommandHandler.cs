using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Handles the AddStallMemberCommand.
/// Validates and adds a new member to the stall, then publishes a domain event.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<AddStallMemberCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class AddStallMemberCommandHandler : ICommandHandler<AddStallMemberCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly IEventBus _eventBus;

    public AddStallMemberCommandHandler(
        IPlayerShopRepository shops,
        IEventBus eventBus)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task<CommandResult> HandleAsync(
        AddStallMemberCommand command,
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
            
            PlayerStallMemberDescriptor descriptor = new(
                command.MemberPersonaId,
                command.MemberDisplayName,
                command.CanManageInventory,
                command.CanConfigureSettings,
                command.CanCollectEarnings);

            PlayerStallDomainResult<PlayerStallMember> domainResult = 
                aggregate.TryAddMember(command.RequestorPersonaId, descriptor);

            if (!domainResult.Success)
            {
                return CommandResult.Fail(domainResult.ErrorMessage ?? "Failed to add member");
            }

            PlayerStallMember member = domainResult.Payload!;

            bool added = _shops.AddMember(command.StallId, member);
            if (!added)
            {
                return CommandResult.Fail($"Failed to persist member for stall {command.StallId}");
            }

            Log.Info("Stall {StallId} added member: {MemberDisplayName} ({MemberPersonaId})",
                command.StallId, command.MemberDisplayName, command.MemberPersonaId);

            // Publish domain event
            StallMemberAddedEvent evt = new()
            {
                StallId = command.StallId,
                MemberPersonaId = command.MemberPersonaId,
                MemberDisplayName = command.MemberDisplayName,
                AddedByPersonaId = command.RequestorPersonaId,
                CanManageInventory = command.CanManageInventory,
                CanConfigureSettings = command.CanConfigureSettings,
                CanCollectEarnings = command.CanCollectEarnings
            };

            await _eventBus.PublishAsync(evt, cancellationToken).ConfigureAwait(false);

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add member to stall {StallId}", command.StallId);
            return CommandResult.Fail($"Failed to add member: {ex.Message}");
        }
    }
}
