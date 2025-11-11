using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

[ServiceBinding(typeof(ICommandHandler<JoinCoinhouseAccountCommand>))]
public class JoinCoinhouseAccountCommandHandler(IPersonaRepository personas, ICoinhouseRepository coinhouses)
    : ICommandHandler<JoinCoinhouseAccountCommand>
{
    public async Task<CommandResult> HandleAsync(JoinCoinhouseAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveHolderCharacter(command.Requestor, out Guid holderId))
        {
            return CommandResult.Fail("Only active characters may join coinhouse accounts.");
        }

        CoinhouseAccountDto? coinhouse = await coinhouses.GetAccountForAsync(command.AccountId, cancellationToken);
        if (coinhouse is null)
        {
            return CommandResult.Fail("The specified coinhouse account could not be found.");
        }

        List<CoinhouseAccountHolderDto> holders = coinhouse.Holders.ToList();

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
