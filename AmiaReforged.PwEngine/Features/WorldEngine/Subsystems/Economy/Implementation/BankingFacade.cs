using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation;

/// <summary>
/// Implementation of the Banking Gateway.
/// Routes operations through the central command/query dispatchers so writes get
/// logging, the exception-to-Fail contract, and CommandExecutedEvent publishing.
/// </summary>
[ServiceBinding(typeof(IBankingFacade))]
public sealed class BankingFacade : IBankingFacade
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public BankingFacade(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <inheritdoc />
    public Task<CommandResult> OpenCoinhouseAccountAsync(OpenCoinhouseAccountCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CoinhouseAccountQueryResult?> GetCoinhouseAccountAsync(GetCoinhouseAccountQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>(query, ct);

    /// <inheritdoc />
    public Task<AccessibleAccountsResult> GetAccessibleAccountsAsync(GetAccessibleAccountsQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetAccessibleAccountsQuery, AccessibleAccountsResult>(query, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<BalanceDto>> GetCoinhouseBalancesAsync(GetCoinhouseBalancesQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>>(query, ct);

    /// <inheritdoc />
    public Task<CoinhouseAccountEligibilityResult> GetCoinhouseAccountEligibilityAsync(
        GetCoinhouseAccountEligibilityQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> DepositGoldAsync(DepositGoldCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> WithdrawGoldAsync(WithdrawGoldCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<int?> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetBalanceQuery, int?>(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> JoinCoinhouseAccountAsync(JoinCoinhouseAccountCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> RemoveAccountHolderAsync(RemoveCoinhouseAccountHolderCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> UpdateAccountHolderRoleAsync(UpdateCoinhouseAccountHolderRoleCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);
}
