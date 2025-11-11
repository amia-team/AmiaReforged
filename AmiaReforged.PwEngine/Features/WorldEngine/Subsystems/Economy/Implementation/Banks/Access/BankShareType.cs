using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

public enum BankShareType
{
    JointOwner = 1,
    AuthorizedUser = 2,
    Trustee = 3
}

public static class BankShareTypeExtensions
{
    public static HolderRole ToHolderRole(this BankShareType shareType)
    {
        return shareType switch
        {
            BankShareType.JointOwner => HolderRole.JointOwner,
            BankShareType.AuthorizedUser => HolderRole.AuthorizedUser,
            BankShareType.Trustee => HolderRole.Trustee,
            _ => HolderRole.Viewer
        };
    }
}
