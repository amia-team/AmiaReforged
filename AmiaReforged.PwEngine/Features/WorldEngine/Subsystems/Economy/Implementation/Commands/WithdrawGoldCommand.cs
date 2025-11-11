using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;

/// <summary>
/// Command to withdraw gold from a coinhouse.
/// Immutable record enforcing validation through factory method.
/// </summary>
public sealed record WithdrawGoldCommand : ICommand
{
    public required PersonaId PersonaId { get; init; }
    public required CoinhouseTag Coinhouse { get; init; }
    public required GoldAmount Amount { get; init; }
    public required TransactionReason Reason { get; init; }

    /// <summary>
    /// Creates a validated WithdrawGoldCommand.
    /// </summary>
    /// <param name="personaId">The persona withdrawing the gold</param>
    /// <param name="coinhouse">The coinhouse to withdraw from</param>
    /// <param name="amount">The amount of gold to withdraw (must be >= 0)</param>
    /// <param name="reason">The reason for the withdrawal (3-200 characters)</param>
    /// <returns>A validated command</returns>
    /// <exception cref="ArgumentException">If validation fails</exception>
    public static WithdrawGoldCommand Create(
        PersonaId personaId,
        CoinhouseTag coinhouse,
        int amount,
        string reason)
    {
        return new WithdrawGoldCommand
        {
            PersonaId = personaId,
            Coinhouse = coinhouse,
            Amount = GoldAmount.Parse(amount),
            Reason = TransactionReason.Parse(reason)
        };
    }
}

