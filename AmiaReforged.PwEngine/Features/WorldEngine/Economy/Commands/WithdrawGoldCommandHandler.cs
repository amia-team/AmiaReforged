using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;

/// <summary>
/// Handles the execution of WithdrawGoldCommand.
/// Validates the withdrawal, checks sufficient balance, updates the coinhouse account,
/// records the transaction, and publishes a GoldWithdrawnEvent.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<WithdrawGoldCommand>))]
public class WithdrawGoldCommandHandler : ICommandHandler<WithdrawGoldCommand>
{
    private readonly ICoinhouseRepository _coinhouses;
    private readonly ITransactionRepository _transactions;
    private readonly IEventBus _eventBus;

    public WithdrawGoldCommandHandler(
        ICoinhouseRepository coinhouses,
        ITransactionRepository transactions,
        IEventBus eventBus)
    {
        _coinhouses = coinhouses;
        _transactions = transactions;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(
        WithdrawGoldCommand command,
        CancellationToken cancellationToken = default)
    {
        // Check cancellation immediately
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Validate coinhouse exists
            CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(command.Coinhouse, cancellationToken);
            if (coinhouse == null)
            {
                return CommandResult.Fail($"Coinhouse '{command.Coinhouse.Value}' not found");
            }

            // Get account (must exist for withdrawal)
            Guid accountId = PersonaAccountId.ForCoinhouse(command.PersonaId, command.Coinhouse);
            CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

            if (account == null)
            {
                return CommandResult.Fail(
                    $"No account found for persona '{command.PersonaId}' at coinhouse '{command.Coinhouse.Value}'");
            }

            // Check sufficient balance
            int currentBalance = account.Debit;
            if (currentBalance < command.Amount.Value)
            {
                return CommandResult.Fail(
                    $"Insufficient balance. Current: {currentBalance} gold, Requested: {command.Amount.Value} gold");
            }

            // Check cancellation before mutation
            cancellationToken.ThrowIfCancellationRequested();

            // Update account balance
            account = account with
            {
                Debit = account.Debit - command.Amount.Value,
                LastAccessedAt = DateTime.UtcNow
            };

            await _coinhouses.SaveAccountAsync(account, cancellationToken);

            // Record transaction
            Transaction transaction = new Transaction
            {
                FromPersonaId = coinhouse.Persona.ToString(),
                ToPersonaId = command.PersonaId.ToString(),
                Amount = command.Amount.Value,
                Memo = $"Withdrawal: {command.Reason.Value}",
                Timestamp = DateTime.UtcNow
            };

            Transaction recordedTransaction = await _transactions.RecordTransactionAsync(
                transaction,
                cancellationToken);

            // Publish event
            GoldWithdrawnEvent evt = new GoldWithdrawnEvent(
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
            return CommandResult.Fail($"Failed to withdraw gold: {ex.Message}");
        }
    }

}

