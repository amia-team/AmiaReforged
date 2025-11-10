using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations.Gateways;

/// <summary>
/// Implementation of the Banking Gateway.
/// Delegates to existing command and query handlers for banking operations.
/// </summary>
[ServiceBinding(typeof(IBankingGateway))]
public sealed class BankingGateway : IBankingGateway
{
    private readonly ICommandHandler<OpenCoinhouseAccountCommand> _openAccountHandler;
    private readonly IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> _getAccountHandler;
    private readonly IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>> _getBalancesHandler;
    private readonly IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> _eligibilityHandler;
    private readonly ICommandHandler<DepositGoldCommand> _depositHandler;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;
    private readonly IQueryHandler<GetBalanceQuery, int?> _getBalanceHandler;

    public BankingGateway(
        ICommandHandler<OpenCoinhouseAccountCommand> openAccountHandler,
        IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> getAccountHandler,
        IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>> getBalancesHandler,
        IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> eligibilityHandler,
        ICommandHandler<DepositGoldCommand> depositHandler,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler,
        IQueryHandler<GetBalanceQuery, int?> getBalanceHandler)
    {
        _openAccountHandler = openAccountHandler;
        _getAccountHandler = getAccountHandler;
        _getBalancesHandler = getBalancesHandler;
        _eligibilityHandler = eligibilityHandler;
        _depositHandler = depositHandler;
        _withdrawHandler = withdrawHandler;
        _getBalanceHandler = getBalanceHandler;
    }

    public Task<CommandResult> OpenCoinhouseAccountAsync(OpenCoinhouseAccountCommand command, CancellationToken ct = default)
        => _openAccountHandler.HandleAsync(command, ct);

    public Task<CoinhouseAccountQueryResult?> GetCoinhouseAccountAsync(GetCoinhouseAccountQuery query, CancellationToken ct = default)
        => _getAccountHandler.HandleAsync(query, ct);

    public Task<IReadOnlyList<BalanceDto>> GetCoinhouseBalancesAsync(GetCoinhouseBalancesQuery query, CancellationToken ct = default)
        => _getBalancesHandler.HandleAsync(query, ct);

    public Task<CoinhouseAccountEligibilityResult> GetCoinhouseAccountEligibilityAsync(
        GetCoinhouseAccountEligibilityQuery query, CancellationToken ct = default)
        => _eligibilityHandler.HandleAsync(query, ct);

    public Task<CommandResult> DepositGoldAsync(DepositGoldCommand command, CancellationToken ct = default)
        => _depositHandler.HandleAsync(command, ct);

    public Task<CommandResult> WithdrawGoldAsync(WithdrawGoldCommand command, CancellationToken ct = default)
        => _withdrawHandler.HandleAsync(command, ct);

    public Task<int?> GetBalanceAsync(GetBalanceQuery query, CancellationToken ct = default)
        => _getBalanceHandler.HandleAsync(query, ct);
}

