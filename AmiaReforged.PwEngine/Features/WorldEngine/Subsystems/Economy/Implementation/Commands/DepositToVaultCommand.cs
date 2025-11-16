using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
/// <summary>
/// Command to deposit gold into a vault for a character in a specific area.
/// Vaults are ad-hoc storage for held funds (e.g., from suspended stalls).
/// </summary>
public sealed record DepositToVaultCommand : ICommand
{
    public required CharacterId Owner { get; init; }
    public required string AreaResRef { get; init; }
    public required int Amount { get; init; }
    public required string Reason { get; init; }
    public static DepositToVaultCommand Create(CharacterId owner, string areaResRef, int amount, string reason)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(areaResRef))
            throw new ArgumentException("Area ResRef cannot be empty.", nameof(areaResRef));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be empty.", nameof(reason));
        return new DepositToVaultCommand
        {
            Owner = owner,
            AreaResRef = areaResRef,
            Amount = amount,
            Reason = reason
        };
    }
}
