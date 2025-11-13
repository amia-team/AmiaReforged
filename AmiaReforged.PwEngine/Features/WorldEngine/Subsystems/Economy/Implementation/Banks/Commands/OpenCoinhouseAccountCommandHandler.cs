using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;

/// <summary>
/// Handles creation of coinhouse accounts initiated from the banking UI.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<OpenCoinhouseAccountCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class OpenCoinhouseAccountCommandHandler : ICommandHandler<OpenCoinhouseAccountCommand>
{
    private readonly ICoinhouseRepository _coinhouses;
    private readonly IOrganizationMemberRepository _organizationMembers;
    private readonly IOrganizationRepository _organizations;

    public OpenCoinhouseAccountCommandHandler(
        ICoinhouseRepository coinhouses,
        IOrganizationMemberRepository organizationMembers,
        IOrganizationRepository organizations)
    {
        _coinhouses = coinhouses;
        _organizationMembers = organizationMembers;
        _organizations = organizations;
    }

    public async Task<CommandResult> HandleAsync(
        OpenCoinhouseAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(command.Coinhouse, cancellationToken);
        if (coinhouse is null)
        {
            return CommandResult.Fail($"Coinhouse '{command.Coinhouse.Value}' could not be found.");
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(command.AccountPersona, command.Coinhouse);
        CoinhouseAccountDto? existingAccount = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);
        if (existingAccount is not null)
        {
            return CommandResult.Fail("An account already exists for this persona at the selected coinhouse.");
        }

        CommandResult validationResult = await ValidateRequestorAsync(command, cancellationToken);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        IReadOnlyList<CoinhouseAccountHolderDto> holders = await BuildHolderListAsync(command, coinhouse, cancellationToken);
        if (holders.Count == 0)
        {
            return CommandResult.Fail("Unable to determine account ownership details.");
        }

        DateTime timestamp = DateTime.UtcNow;

        CoinhouseAccountDto account = new()
        {
            Id = accountId,
            Debit = 0,
            Credit = 0,
            CoinHouseId = coinhouse.Id,
            OpenedAt = timestamp,
            LastAccessedAt = timestamp,
            Coinhouse = coinhouse,
            Holders = holders
        };

        await _coinhouses.SaveAccountAsync(account, cancellationToken);

        return CommandResult.OkWith("AccountId", account.Id);
    }

    private async Task<CommandResult> ValidateRequestorAsync(
        OpenCoinhouseAccountCommand command,
        CancellationToken cancellationToken)
    {
        if (command.AccountPersona.Type == PersonaType.Character)
        {
            if (!string.Equals(command.Requestor.Value, command.AccountPersona.Value, StringComparison.OrdinalIgnoreCase))
            {
                return CommandResult.Fail("Only the character themselves may open a personal coinhouse account.");
            }

            return CommandResult.Ok();
        }

        if (command.AccountPersona.Type == PersonaType.Organization)
        {
            if (!Guid.TryParse(command.AccountPersona.Value, out Guid organizationGuid))
            {
                return CommandResult.Fail("Organization identifier is invalid.");
            }

            if (!Guid.TryParse(command.Requestor.Value, out Guid requestorGuid))
            {
                return CommandResult.Fail("Requestor must be a valid character persona.");
            }

            OrganizationId organizationId = OrganizationId.From(organizationGuid);
            CharacterId characterId = CharacterId.From(requestorGuid);
            OrganizationMember? membership = _organizationMembers
                .GetByCharacterAndOrganization(characterId, organizationId);

            if (membership is null || membership.Status != MembershipStatus.Active || !membership.IsLeader())
            {
                return CommandResult.Fail("Only active organization leaders may open organization accounts.");
            }

            return CommandResult.Ok();
        }

        return CommandResult.Fail("Only character or organization personas may open coinhouse accounts.");
    }

    private async Task<IReadOnlyList<CoinhouseAccountHolderDto>> BuildHolderListAsync(
        OpenCoinhouseAccountCommand command,
        CoinhouseDto coinhouse,
        CancellationToken cancellationToken)
    {
        List<CoinhouseAccountHolderDto> holders = new();
        HashSet<Guid> holderIds = new();

        if (TryCreatePrimaryHolder(command, coinhouse, out CoinhouseAccountHolderDto? primaryHolder))
        {
            holders.Add(primaryHolder);
            holderIds.Add(primaryHolder.HolderId);
        }

        if (command.AdditionalHolders is { Count: > 0 })
        {
            foreach (CoinhouseAccountHolderDto additional in command.AdditionalHolders)
            {
                if (holderIds.Contains(additional.HolderId))
                {
                    continue;
                }

                holders.Add(additional);
                holderIds.Add(additional.HolderId);
            }
        }

        return holders;
    }

    private bool TryCreatePrimaryHolder(
        OpenCoinhouseAccountCommand command,
        CoinhouseDto coinhouse,
        out CoinhouseAccountHolderDto? holder)
    {
        holder = null;

        if (!Guid.TryParse(command.AccountPersona.Value, out Guid accountPersonaGuid))
        {
            return false;
        }

        HolderType holderType = command.AccountPersona.Type switch
        {
            PersonaType.Organization => HolderType.Organization,
            PersonaType.Government => HolderType.Government,
            _ => HolderType.Individual
        };

        string displayName = ResolveDisplayName(command, coinhouse);

        holder = new CoinhouseAccountHolderDto
        {
            HolderId = accountPersonaGuid,
            Type = holderType,
            Role = HolderRole.Owner,
            FirstName = displayName,
            LastName = string.Empty
        };

        return true;
    }

    private string ResolveDisplayName(OpenCoinhouseAccountCommand command, CoinhouseDto coinhouse)
    {
        if (!string.IsNullOrWhiteSpace(command.AccountDisplayName))
        {
            return command.AccountDisplayName.Trim();
        }

        if (command.AccountPersona.Type == PersonaType.Organization
            && Guid.TryParse(command.AccountPersona.Value, out Guid organizationGuid))
        {
            IOrganization? organization = _organizations.GetById(OrganizationId.From(organizationGuid));
            if (organization?.Name is { Length: > 0 })
            {
                return organization.Name;
            }
        }

        if (command.AccountPersona.Type == PersonaType.Character
            && Guid.TryParse(command.AccountPersona.Value, out _))
        {
            return command.AccountPersona.Value;
        }

        return coinhouse.Tag.Value ?? "Coinhouse Account";
    }
}
