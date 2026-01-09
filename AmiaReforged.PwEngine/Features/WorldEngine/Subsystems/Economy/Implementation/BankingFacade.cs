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
/// Delegates to existing command and query handlers for banking operations.
/// </summary>
[ServiceBinding(typeof(IBankingFacade))]
public sealed class BankingFacade : IBankingFacade
{
    private readonly ICommandHandler<OpenCoinhouseAccountCommand> _openAccountHandler;
    private readonly IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> _getAccountHandler;
    private readonly IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>> _getBalancesHandler;
    private readonly IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> _eligibilityHandler;
    private readonly ICommandHandler<DepositGoldCommand> _depositHandler;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;
    private readonly IQueryHandler<GetBalanceQuery, int?> _getBalanceHandler;
    private readonly ICommandHandler<JoinCoinhouseAccountCommand> _joinAccountHandler;
    private readonly ICommandHandler<RemoveCoinhouseAccountHolderCommand> _removeHolderHandler;
    private readonly ICommandHandler<UpdateCoinhouseAccountHolderRoleCommand> _updateHolderRoleHandler;

    public BankingFacade(
        ICommandHandler<OpenCoinhouseAccountCommand> openAccountHandler,
        IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> getAccountHandler,
        IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>> getBalancesHandler,
        IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> eligibilityHandler,
        ICommandHandler<DepositGoldCommand> depositHandler,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler,
        IQueryHandler<GetBalanceQuery, int?> getBalanceHandler,
        ICommandHandler<JoinCoinhouseAccountCommand> joinAccountHandler,
        ICommandHandler<RemoveCoinhouseAccountHolderCommand> removeHolderHandler,
        ICommandHandler<UpdateCoinhouseAccountHolderRoleCommand> updateHolderRoleHandler)
    {
        _openAccountHandler = openAccountHandler;
        _getAccountHandler = getAccountHandler;
        _getBalancesHandler = getBalancesHandler;
        _eligibilityHandler = eligibilityHandler;
        _depositHandler = depositHandler;
        _withdrawHandler = withdrawHandler;
        _getBalanceHandler = getBalanceHandler;
        _joinAccountHandler = joinAccountHandler;
        _removeHolderHandler = removeHolderHandler;
        _updateHolderRoleHandler = updateHolderRoleHandler;
    }

    /// <inheritdoc />
    public Task<CommandResult> OpenCoinhouseAccountAsync(OpenCoinhouseAccountCommand command, CancellationToken ct = default)
        => _openAccountHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CoinhouseAccountQueryResult?> GetCoinhouseAccountAsync(GetCoinhouseAccountQuery query, CancellationToken ct = default)
        => _getAccountHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<IReadOnlyList<BalanceDto>> GetCoinhouseBalancesAsync(GetCoinhouseBalancesQuery query, CancellationToken ct = default)
        => _getBalancesHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<CoinhouseAccountEligibilityResult> GetCoinhouseAccountEligibilityAsync(
        GetCoinhouseAccountEligibilityQuery query, CancellationToken ct = default)
        => _eligibilityHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> DepositGoldAsync(DepositGoldCommand command, CancellationToken ct = default)
        => _depositHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> WithdrawGoldAsync(WithdrawGoldCommand command, CancellationToken ct = default)
        => _withdrawHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<int?> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct = default)
        => _getBalanceHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> JoinCoinhouseAccountAsync(JoinCoinhouseAccountCommand command, CancellationToken ct = default)
        => _joinAccountHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> RemoveAccountHolderAsync(RemoveCoinhouseAccountHolderCommand command, CancellationToken ct = default)
        => _removeHolderHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> UpdateAccountHolderRoleAsync(UpdateCoinhouseAccountHolderRoleCommand command, CancellationToken ct = default)
        => _updateHolderRoleHandler.HandleAsync(command, ct);
}

