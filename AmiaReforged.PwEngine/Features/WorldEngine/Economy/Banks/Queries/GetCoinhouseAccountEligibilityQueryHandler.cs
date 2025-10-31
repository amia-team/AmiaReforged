using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;

/// <summary>
/// Evaluates whether a persona can open a personal or organization-backed account at a coinhouse.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>))]
public sealed class GetCoinhouseAccountEligibilityQueryHandler
    : IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>
{
    private readonly ICoinhouseRepository _coinhouses;
    private readonly IOrganizationMemberRepository _organizationMembers;
    private readonly IOrganizationRepository _organizations;

    public GetCoinhouseAccountEligibilityQueryHandler(
        ICoinhouseRepository coinhouses,
        IOrganizationMemberRepository organizationMembers,
        IOrganizationRepository organizations)
    {
        _coinhouses = coinhouses;
        _organizationMembers = organizationMembers;
        _organizations = organizations;
    }

    public async Task<CoinhouseAccountEligibilityResult> HandleAsync(
        GetCoinhouseAccountEligibilityQuery query,
        CancellationToken cancellationToken = default)
    {
        CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(query.Coinhouse, cancellationToken);
        if (coinhouse is null)
        {
            return CoinhouseAccountEligibilityResult.CoinhouseUnavailable(
                $"Coinhouse '{query.Coinhouse.Value}' could not be found.");
        }

        bool canOpenPersonalAccount = false;
        string? personalAccountReason = null;
        IReadOnlyList<OrganizationAccountEligibility> organizationOptions = Array.Empty<OrganizationAccountEligibility>();

        switch (query.Persona.Type)
        {
            case PersonaType.Character:
                (canOpenPersonalAccount, personalAccountReason) = await EvaluatePersonalEligibilityAsync(
                    query.Persona,
                    query.Coinhouse,
                    cancellationToken);

                organizationOptions = await EvaluateOrganizationOptionsAsync(
                    query.Persona,
                    query.Coinhouse,
                    cancellationToken);
                break;
            case PersonaType.Organization:
                personalAccountReason = "Organization personas must open accounts via a character with leader privileges.";
                break;
            default:
                personalAccountReason = "Only characters may open personal coinhouse accounts.";
                break;
        }

        return new CoinhouseAccountEligibilityResult
        {
            CoinhouseExists = true,
            CanOpenPersonalAccount = canOpenPersonalAccount,
            PersonalAccountBlockedReason = canOpenPersonalAccount ? null : personalAccountReason,
            Organizations = organizationOptions
        };
    }

    private async Task<(bool canOpen, string? reason)> EvaluatePersonalEligibilityAsync(
        PersonaId persona,
        CoinhouseTag coinhouse,
        CancellationToken cancellationToken)
    {
        Guid accountId = PersonaAccountId.ForCoinhouse(persona, coinhouse);
        CoinhouseAccountDto? existingAccount = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

        if (existingAccount is not null)
        {
            return (false, "You already maintain an account at this coinhouse.");
        }

    if (!Guid.TryParse(persona.Value, out _))
        {
            return (false, "Unable to resolve the requesting persona identifier.");
        }

        // A personal account may be opened when no existing coinhouse account is present.
        return (true, null);
    }

    private async Task<IReadOnlyList<OrganizationAccountEligibility>> EvaluateOrganizationOptionsAsync(
        PersonaId persona,
        CoinhouseTag coinhouse,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(persona.Value, out Guid characterGuid))
        {
            return Array.Empty<OrganizationAccountEligibility>();
        }

        CharacterId characterId = CharacterId.From(characterGuid);
        List<OrganizationMember>? memberships = _organizationMembers.GetByCharacter(characterId);
        if (memberships is null || memberships.Count == 0)
        {
            return Array.Empty<OrganizationAccountEligibility>();
        }

        HashSet<OrganizationId> processed = new();
        List<OrganizationAccountEligibility> options = new();

        foreach (OrganizationMember membership in memberships)
        {
            if (membership.Status != MembershipStatus.Active || !membership.IsLeader())
            {
                continue;
            }

            if (!processed.Add(membership.OrganizationId))
            {
                continue;
            }

            IOrganization? organization = _organizations.GetById(membership.OrganizationId);
            if (organization is null)
            {
                continue;
            }

            PersonaId organizationPersona = PersonaId.FromOrganization(membership.OrganizationId);
            Guid organizationAccountId = PersonaAccountId.ForCoinhouse(organizationPersona, coinhouse);
            CoinhouseAccountDto? organizationAccount = await _coinhouses.GetAccountForAsync(
                organizationAccountId,
                cancellationToken);

            bool canOpen = organizationAccount is null;
            string? reason = canOpen
                ? null
                : $"{organization.Name} already maintains an account at this coinhouse.";

            options.Add(new OrganizationAccountEligibility
            {
                OrganizationId = membership.OrganizationId,
                OrganizationName = organization.Name,
                CanOpen = canOpen,
                AlreadyHasAccount = !canOpen,
                BlockedReason = reason
            });
        }

        return options;
    }
}
