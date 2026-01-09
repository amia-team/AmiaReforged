using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Handles removal of account holders from coinhouse accounts.
/// Includes protection against removing the sole owner.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<RemoveCoinhouseAccountHolderCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class RemoveCoinhouseAccountHolderCommandHandler(
    IPersonaRepository personas,
    ICoinhouseRepository coinhouses,
    IEventBus eventBus)
    : ICommandHandler<RemoveCoinhouseAccountHolderCommand>
{
    public async Task<CommandResult> HandleAsync(RemoveCoinhouseAccountHolderCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validate requestor is a valid character persona
        if (!TryResolveHolderCharacter(command.Requestor, out Guid requestorId))
        {
            return CommandResult.Fail("Only active characters may manage coinhouse account holders.");
        }

        // Load the account
        CoinhouseAccountDto? account = await coinhouses.GetAccountForAsync(command.AccountId, cancellationToken);
        if (account is null)
        {
            return CommandResult.Fail("The specified coinhouse account could not be found.");
        }

        List<CoinhouseAccountHolderDto> holders = account.Holders.ToList();

        // Find the holder to remove
        CoinhouseAccountHolderDto? holderToRemove = holders.FirstOrDefault(h => h.HolderId == command.HolderIdToRemove);
        if (holderToRemove is null)
        {
            return CommandResult.Fail("The specified holder is not a member of this account.");
        }

        // Verify the requestor has permission to manage holders
        CoinhouseAccountHolderDto? requestorHolder = holders.FirstOrDefault(h => h.HolderId == requestorId);
        if (requestorHolder is null)
        {
            return CommandResult.Fail("You are not a holder on this account.");
        }

        if (!CanManageHolders(requestorHolder.Role))
        {
            return CommandResult.Fail("You do not have permission to manage account holders.");
        }

        // Prevent removing the sole owner
        bool isRemovingOwner = holderToRemove.Role == HolderRole.Owner;
        if (isRemovingOwner)
        {
            int ownerCount = holders.Count(h => h.Role == HolderRole.Owner);
            if (ownerCount <= 1)
            {
                return CommandResult.Fail("Cannot remove the sole owner of the account. Transfer ownership first or close the account.");
            }
        }

        // Remove the holder
        holders.Remove(holderToRemove);

        CoinhouseAccountDto updatedAccount = account with
        {
            Holders = holders
        };

        await coinhouses.SaveAccountAsync(updatedAccount, cancellationToken);

        // Publish audit event
        string holderName = $"{holderToRemove.FirstName} {holderToRemove.LastName}".Trim();
        AccountHolderRemovedEvent removedEvent = new(
            command.AccountId,
            command.CoinhouseTag,
            command.HolderIdToRemove,
            holderName,
            holderToRemove.Role,
            command.Requestor);

        await eventBus.PublishAsync(removedEvent, cancellationToken);

        return CommandResult.Ok();
    }

    private static bool CanManageHolders(HolderRole role)
    {
        return role is HolderRole.Owner or HolderRole.JointOwner;
    }

    private bool TryResolveHolderCharacter(PersonaId personaId, out Guid characterId)
    {
        characterId = Guid.Empty;

        if (personaId.Type != PersonaType.Character)
            return false;

        if (!Guid.TryParse(personaId.Value, out Guid parsed))
            return false;

        if (!personas.Exists(personaId))
            return false;

        characterId = parsed;
        return true;
    }
}
