using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;

/// <summary>
/// Gateway for banking and coinhouse operations.
/// Provides access to account management and gold transactions.
/// </summary>
public interface IBankingFacade
{
    // === Account Management ===

    /// <summary>
    /// Opens a new coinhouse account for a persona.
    /// </summary>
    Task<CommandResult> OpenCoinhouseAccountAsync(OpenCoinhouseAccountCommand command, CancellationToken ct = default);

    /// <summary>
    /// Gets coinhouse account details for a persona.
    /// </summary>
    Task<CoinhouseAccountQueryResult?> GetCoinhouseAccountAsync(GetCoinhouseAccountQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets all coinhouse balances for a persona.
    /// </summary>
    Task<IReadOnlyList<BalanceDto>> GetCoinhouseBalancesAsync(GetCoinhouseBalancesQuery query, CancellationToken ct = default);

    /// <summary>
    /// Checks eligibility for opening a coinhouse account.
    /// </summary>
    Task<CoinhouseAccountEligibilityResult> GetCoinhouseAccountEligibilityAsync(
        GetCoinhouseAccountEligibilityQuery query, CancellationToken ct = default);

    // === Gold Transactions ===

    /// <summary>
    /// Deposits gold into a coinhouse account.
    /// </summary>
    Task<CommandResult> DepositGoldAsync(DepositGoldCommand command, CancellationToken ct = default);

    /// <summary>
    /// Withdraws gold from a coinhouse account.
    /// </summary>
    Task<CommandResult> WithdrawGoldAsync(WithdrawGoldCommand command, CancellationToken ct = default);

    /// <summary>
    /// Gets the gold balance for a persona at a specific coinhouse.
    /// </summary>
    Task<int?> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct = default);
}

