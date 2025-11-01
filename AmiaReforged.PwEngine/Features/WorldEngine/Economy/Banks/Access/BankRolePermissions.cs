using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Access;

/// <summary>
/// Maps bank holder roles to their effective permissions.
/// </summary>
public static class BankRolePermissions
{
    private static readonly Dictionary<HolderRole, BankPermission> RoleMap = new()
    {
        [HolderRole.Owner] = BankPermission.View | BankPermission.Deposit | BankPermission.Withdraw | BankPermission.IssueShares,
        [HolderRole.JointOwner] = BankPermission.View | BankPermission.Deposit | BankPermission.Withdraw,
        [HolderRole.AuthorizedUser] = BankPermission.View | BankPermission.Deposit | BankPermission.Withdraw,
        [HolderRole.Signatory] = BankPermission.View | BankPermission.Deposit | BankPermission.Withdraw,
        [HolderRole.Trustee] = BankPermission.View,
        [HolderRole.Viewer] = BankPermission.View
    };

    public static BankPermission ForRole(HolderRole role)
    {
        return RoleMap.TryGetValue(role, out BankPermission permissions)
            ? permissions
            : BankPermission.View;
    }
}
