using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

/// <summary>
/// Encapsulates the evaluated permissions a player has for a specific bank account.
/// </summary>
public sealed record BankAccessProfile
{
    public static BankAccessProfile None { get; } = new(BankPermission.None, null, null);

    public BankAccessProfile(BankPermission permissions, HolderRole? holderRole, Guid? organizationId)
    {
        Permissions = permissions;
        HolderRole = holderRole;
        OrganizationId = organizationId;
    }

    public BankPermission Permissions { get; }

    public HolderRole? HolderRole { get; }

    public Guid? OrganizationId { get; }

    public bool IsOrganizationAccount => OrganizationId.HasValue;

    public bool CanView => Permissions.HasFlag(BankPermission.View);

    public bool CanDeposit => Permissions.HasFlag(BankPermission.Deposit);

    public bool CanWithdraw => Permissions.HasFlag(BankPermission.Withdraw);

    public bool CanRequestWithdraw => Permissions.HasFlag(BankPermission.RequestWithdraw);

    public bool CanIssueShares => Permissions.HasFlag(BankPermission.IssueShares);

    public bool CanManageHolders => Permissions.HasFlag(BankPermission.ManageHolders);
}
