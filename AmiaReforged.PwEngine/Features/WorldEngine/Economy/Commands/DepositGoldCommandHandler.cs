using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;

/// <summary>
/// Handles the execution of DepositGoldCommand.
/// Validates the deposit, updates the coinhouse account, records the transaction,
/// and publishes a GoldDepositedEvent.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<DepositGoldCommand>))]
public class DepositGoldCommandHandler : ICommandHandler<DepositGoldCommand>
{
    private readonly ICoinhouseRepository _coinhouses;
    private readonly ITransactionRepository _transactions;
    private readonly IEventBus _eventBus;

    public DepositGoldCommandHandler(
        ICoinhouseRepository coinhouses,
        ITransactionRepository transactions,
        IEventBus eventBus)
    {
        _coinhouses = coinhouses;
        _transactions = transactions;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(
        DepositGoldCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate coinhouse exists
            CoinHouse? coinhouse = _coinhouses.GetByTag(command.Coinhouse);
            if (coinhouse == null)
            {
                return CommandResult.Fail($"Coinhouse '{command.Coinhouse.Value}' not found");
            }

            // Get or create account
            Guid accountId = PersonaAccountId.From(command.PersonaId);
            CoinHouseAccount? account = _coinhouses.GetAccountFor(accountId);

            if (account == null)
            {
                account = CreateNewAccount(accountId, coinhouse);
                coinhouse.Accounts ??= new List<CoinHouseAccount>();
                coinhouse.Accounts.Add(account);
            }

            // Check cancellation before mutation
            cancellationToken.ThrowIfCancellationRequested();

            // Update account balance
            account.Debit += command.Amount.Value;
            account.LastAccessedAt = DateTime.UtcNow;

            // Record transaction
            Transaction transaction = new Transaction
            {
                FromPersonaId = command.PersonaId.ToString(),
                ToPersonaId = coinhouse.PersonaId.ToString(),
                Amount = command.Amount.Value,
                Memo = $"Deposit: {command.Reason.Value}",
                Timestamp = DateTime.UtcNow
            };

            Transaction recordedTransaction = await _transactions.RecordTransactionAsync(
                transaction,
                cancellationToken);

            // Publish event
            GoldDepositedEvent evt = new GoldDepositedEvent(
                command.PersonaId,
                command.Coinhouse,
                command.Amount,
                TransactionId.NewId(), // TODO: Use DB transaction ID once we switch to Guid
                recordedTransaction.Timestamp);

            await _eventBus.PublishAsync(evt, cancellationToken);

            return CommandResult.OkWith("transactionId", recordedTransaction.Id);
        }
        catch (OperationCanceledException)
        {
            throw; // Propagate cancellation
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"Failed to deposit gold: {ex.Message}");
        }
    }

    private static CoinHouseAccount CreateNewAccount(Guid accountId, CoinHouse coinhouse)
    {
        return new CoinHouseAccount
        {
            Id = accountId,
            Debit = 0,
            Credit = 0,
            CoinHouseId = coinhouse.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
    }
}

