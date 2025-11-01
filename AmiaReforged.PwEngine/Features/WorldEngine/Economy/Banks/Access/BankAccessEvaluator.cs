using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Access;

[ServiceBinding(typeof(IBankAccessEvaluator))]
public sealed class BankAccessEvaluator : IBankAccessEvaluator
{
    private readonly IOrganizationMemberRepository _organizationMembers;

    public BankAccessEvaluator(IOrganizationMemberRepository organizationMembers)
    {
        _organizationMembers = organizationMembers;
    }

    public BankAccessProfile Evaluate(
        PersonaId viewerPersona,
        CoinhouseAccountSummary accountSummary,
        IReadOnlyList<CoinhouseAccountHolderDto> holders)
    {
        if (accountSummary is null)
        {
            throw new ArgumentNullException(nameof(accountSummary));
        }

        if (holders is null)
        {
            throw new ArgumentNullException(nameof(holders));
        }

        BankPermission permissions = BankPermission.None;
        HolderRole? holderRole = null;
        Guid? organizationId = null;

        bool viewerHasGuid = Guid.TryParse(viewerPersona.Value, out Guid viewerGuid);

        CoinhouseAccountHolderDto? organizationOwner = holders
            .FirstOrDefault(h => h.Role == HolderRole.Owner && h.Type == HolderType.Organization);

        if (organizationOwner is not null)
        {
            organizationId = organizationOwner.HolderId;
        }

        if (viewerHasGuid)
        {
            CoinhouseAccountHolderDto? directHolder = holders.FirstOrDefault(h => h.HolderId == viewerGuid);
            if (directHolder is not null)
            {
                holderRole = directHolder.Role;
                permissions |= BankRolePermissions.ForRole(directHolder.Role);
            }
        }

        if (holderRole is null && organizationId.HasValue && viewerHasGuid)
        {
            permissions |= EvaluateOrganizationMembership(viewerGuid, organizationId.Value);
        }

        if (permissions == BankPermission.None && holderRole is null)
        {
            return BankAccessProfile.None;
        }

        // Ensure view permission is present when other permissions exist.
        if (permissions != BankPermission.None && !permissions.HasFlag(BankPermission.View))
        {
            permissions |= BankPermission.View;
        }

        return new BankAccessProfile(permissions, holderRole, organizationId);
    }

    private BankPermission EvaluateOrganizationMembership(Guid viewerGuid, Guid organizationGuid)
    {
        CharacterId characterId = CharacterId.From(viewerGuid);
        OrganizationId organizationId = OrganizationId.From(organizationGuid);

        OrganizationMember? membership = _organizationMembers.GetByCharacterAndOrganization(characterId, organizationId);
        if (membership is null || membership.Status != MembershipStatus.Active)
        {
            return BankPermission.None;
        }

        if (membership.IsLeader())
        {
            return BankRolePermissions.ForRole(HolderRole.JointOwner);
        }

        BankPermission permissions = BankPermission.None;

        foreach (MemberRole role in membership.Roles)
        {
            string roleValue = role.Value.ToLowerInvariant();
            if (roleValue == OrganizationBankRoles.CanView)
            {
                permissions |= BankPermission.View;
            }
            else if (roleValue == OrganizationBankRoles.CanDeposit)
            {
                permissions |= BankPermission.View | BankPermission.Deposit;
            }
            else if (roleValue == OrganizationBankRoles.CanRequestWithdraw)
            {
                permissions |= BankPermission.View | BankPermission.RequestWithdraw;
            }
        }

        return permissions;
    }
}
