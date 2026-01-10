using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

[ServiceBinding(typeof(ICommandHandler<JoinCoinhouseAccountCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class JoinCoinhouseAccountCommandHandler(IPersonaRepository personas, ICoinhouseRepository coinhouses)
    : ICommandHandler<JoinCoinhouseAccountCommand>
{
    public async Task<CommandResult> HandleAsync(JoinCoinhouseAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        // Determine who is being added as a holder
        PersonaId holderPersona = command.NewHolder ?? command.Requestor;
        
        if (!TryResolveHolderCharacter(holderPersona, out Guid holderId))
        {
            return CommandResult.Fail("Only active characters may join coinhouse accounts.");
        }

        CoinhouseAccountDto? coinhouse = await coinhouses.GetAccountForAsync(command.AccountId, cancellationToken);
        if (coinhouse is null)
        {
            return CommandResult.Fail("The specified coinhouse account could not be found.");
        }

        List<CoinhouseAccountHolderDto> holders = coinhouse.Holders.ToList();

        // If NewHolder is specified, verify the requestor has permission to add holders
        if (command.NewHolder != null)
        {
            if (!TryResolveHolderCharacter(command.Requestor, out Guid requestorId))
            {
                return CommandResult.Fail("Invalid requestor persona.");
            }

            CoinhouseAccountHolderDto? requestorHolder = holders.FirstOrDefault(h => h.HolderId == requestorId);
            if (requestorHolder == null)
            {
                return CommandResult.Fail("You are not an account holder.");
            }

            BankPermission permissions = BankRolePermissions.ForRole(requestorHolder.Role);
            if (!permissions.HasFlag(BankPermission.IssueShares))
            {
                return CommandResult.Fail("You do not have permission to add account holders.");
            }
        }

        if (holders.Any(h => h.HolderId == holderId))
        {
            return CommandResult.Fail("This character is already listed as an account holder.");
        }

        CoinhouseAccountHolderDto newHolder = new()
        {
            HolderId = holderId,
            FirstName = command.HolderFirstName,
            LastName = command.HolderLastName,
            Type = command.HolderType,
            Role = command.Role
        };

        holders.Add(newHolder);

        CoinhouseAccountDto updatedAccount = coinhouse with
        {
            Holders = holders
        };

        await coinhouses.SaveAccountAsync(updatedAccount, cancellationToken);

        return CommandResult.Ok();
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
