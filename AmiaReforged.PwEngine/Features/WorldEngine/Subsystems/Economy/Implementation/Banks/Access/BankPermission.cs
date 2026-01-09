namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;

[Flags]
public enum BankPermission
{
    None = 0,
    View = 1 << 0,
    Deposit = 1 << 1,
    Withdraw = 1 << 2,
    IssueShares = 1 << 3,
    RequestWithdraw = 1 << 4,
    ManageHolders = 1 << 5
}
