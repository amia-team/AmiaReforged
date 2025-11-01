using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;

[ServiceBinding(typeof(ICommandHandler<JoinCoinhouseAccountCommand>))]
public class JoinCoinhouseAccountCommandHandler(IPersonaRepository personas, ICoinhouseRepository coinhouses)
    : ICommandHandler<JoinCoinhouseAccountCommand>
{
    public async Task<CommandResult> HandleAsync(JoinCoinhouseAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        bool isValid = GetIsValidCharacter(command.Requestor);

        if (!isValid)
        {
            return CommandResult.Fail("The requestor persona is not valid.");
        }

        CoinhouseAccountDto? coinhouse = await coinhouses.GetAccountForAsync(command.AccountId, cancellationToken);
        if (coinhouse is null)
        {
            return CommandResult.Fail("The specified coinhouse account could not be found.");
        }

        List<CoinhouseAccountHolderDto> holders = coinhouse.Holders.ToList();

        Guid id = PersonaId.ToGuid(command.Requestor);
        CoinhouseAccountHolderDto newHolder = new()
        {
            HolderId = id,
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

    private bool GetIsValidCharacter(PersonaId commandRequestor)
    {
        return personas.Exists(commandRequestor);
    }
}
