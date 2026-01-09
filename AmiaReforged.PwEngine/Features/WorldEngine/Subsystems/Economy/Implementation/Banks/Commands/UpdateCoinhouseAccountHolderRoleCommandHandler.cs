using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Handles updating an account holder's role on a coinhouse account.
/// Ownership transfer is not permitted - cannot promote someone to Owner.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<UpdateCoinhouseAccountHolderRoleCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class UpdateCoinhouseAccountHolderRoleCommandHandler(
    IPersonaRepository personas,
    ICoinhouseRepository coinhouses,
    IEventBus eventBus)
    : ICommandHandler<UpdateCoinhouseAccountHolderRoleCommand>
{
    public async Task<CommandResult> HandleAsync(UpdateCoinhouseAccountHolderRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        // Prevent ownership transfer
        if (command.NewRole == HolderRole.Owner)
        {
            return CommandResult.Fail("Ownership transfer is not permitted. The Owner role cannot be assigned to other holders.");
        }

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

        // Find the holder to update
        CoinhouseAccountHolderDto? holderToUpdate = holders.FirstOrDefault(h => h.HolderId == command.HolderIdToUpdate);
        if (holderToUpdate is null)
        {
            return CommandResult.Fail("The specified holder is not a member of this account.");
        }

        // Cannot change the role of an Owner
        if (holderToUpdate.Role == HolderRole.Owner)
        {
            return CommandResult.Fail("The Owner's role cannot be changed. To transfer ownership, close the account and open a new one.");
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

        // Check if the role is actually changing
        if (holderToUpdate.Role == command.NewRole)
        {
            return CommandResult.Fail("The holder already has this role.");
        }

        // Update the holder's role
        HolderRole previousRole = holderToUpdate.Role;
        int holderIndex = holders.IndexOf(holderToUpdate);
        holders[holderIndex] = holderToUpdate with { Role = command.NewRole };

        CoinhouseAccountDto updatedAccount = account with
        {
            Holders = holders
        };

        await coinhouses.SaveAccountAsync(updatedAccount, cancellationToken);

        // Publish audit event
        string holderName = $"{holderToUpdate.FirstName} {holderToUpdate.LastName}".Trim();
        AccountHolderRoleChangedEvent roleChangedEvent = new(
            command.AccountId,
            command.CoinhouseTag,
            command.HolderIdToUpdate,
            holderName,
            previousRole,
            command.NewRole,
            command.Requestor);

        await eventBus.PublishAsync(roleChangedEvent, cancellationToken);

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
