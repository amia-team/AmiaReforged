using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;

/// <summary>
/// Command to deposit gold into a coinhouse.
/// Immutable record enforcing validation through factory method.
/// </summary>
public sealed record DepositGoldCommand
{
    public required PersonaId PersonaId { get; init; }
    public required CoinhouseTag Coinhouse { get; init; }
    public required GoldAmount Amount { get; init; }
    public required TransactionReason Reason { get; init; }

    /// <summary>
    /// Creates a validated DepositGoldCommand.
    /// </summary>
    /// <param name="personaId">The persona depositing the gold</param>
    /// <param name="coinhouse">The coinhouse to deposit into</param>
    /// <param name="amount">The amount of gold to deposit (must be >= 0)</param>
    /// <param name="reason">The reason for the deposit (3-200 characters)</param>
    /// <returns>A validated command</returns>
    /// <exception cref="ArgumentException">If validation fails</exception>
    public static DepositGoldCommand Create(
        PersonaId personaId,
        CoinhouseTag coinhouse,
        int amount,
        string reason)
    {
        return new DepositGoldCommand
        {
            PersonaId = personaId,
            Coinhouse = coinhouse,
            Amount = GoldAmount.Parse(amount),
            Reason = TransactionReason.Parse(reason)
        };
    }
}

