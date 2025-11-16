using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
/// <summary>
/// Command to withdraw gold from a vault for a character in a specific area.
/// Withdrawal is clamped to the available balance.
/// </summary>
public sealed record WithdrawFromVaultCommand : ICommand
{
    public required CharacterId Owner { get; init; }
    public required string AreaResRef { get; init; }
    public required int RequestedAmount { get; init; }
    public required string Reason { get; init; }
    public static WithdrawFromVaultCommand Create(CharacterId owner, string areaResRef, int requestedAmount, string reason)
    {
        if (requestedAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), "Requested amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(areaResRef))
            throw new ArgumentException("Area ResRef cannot be empty.", nameof(areaResRef));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty.", nameof(reason));
        return new WithdrawFromVaultCommand
        {
            Owner = owner,
            AreaResRef = areaResRef,
            RequestedAmount = requestedAmount,
            Reason = reason
        };
    }
}
