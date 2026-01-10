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
    /// Gets all accounts a persona can access at a specific coinhouse (personal + shared).
    /// </summary>
    Task<AccessibleAccountsResult> GetAccessibleAccountsAsync(GetAccessibleAccountsQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets all coinhouse balances for a persona.
    /// </summary>
    Task<IReadOnlyList<BalanceDto>> GetCoinhouseBalancesAsync(GetCoinhouseBalancesQuery query, CancellationToken ct = default);

    /// <summary>
    /// Checks eligibility for opening a coinhouse account.
    /// </summary>
    Task<CoinhouseAccountEligibilityResult> GetCoinhouseAccountEligibilityAsync(
        GetCoinhouseAccountEligibilityQuery query, CancellationToken ct = default);

    // === Account Holder Management ===

    /// <summary>
    /// Adds a new holder to a coinhouse account via share document activation.
    /// </summary>
    Task<CommandResult> JoinCoinhouseAccountAsync(JoinCoinhouseAccountCommand command, CancellationToken ct = default);

    /// <summary>
    /// Removes an account holder from a coinhouse account.
    /// Cannot remove the sole owner of an account.
    /// </summary>
    Task<CommandResult> RemoveAccountHolderAsync(RemoveCoinhouseAccountHolderCommand command, CancellationToken ct = default);

    /// <summary>
    /// Updates an account holder's role on a coinhouse account.
    /// Ownership transfer (promoting to Owner) is not permitted.
    /// </summary>
    Task<CommandResult> UpdateAccountHolderRoleAsync(UpdateCoinhouseAccountHolderRoleCommand command, CancellationToken ct = default);

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

