using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
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
            var coinhouse = _coinhouses.GetByTag(command.Coinhouse);
            if (coinhouse == null)
            {
                return CommandResult.Fail($"Coinhouse '{command.Coinhouse.Value}' not found");
            }

            // Get account (must exist for withdrawal)
            var accountId = ExtractAccountId(command.PersonaId);
            var account = _coinhouses.GetAccountFor(accountId);

            if (account == null)
            {
                return CommandResult.Fail(
                    $"No account found for persona '{command.PersonaId}' at coinhouse '{command.Coinhouse.Value}'");
            }

            // Check sufficient balance
            var currentBalance = account.Debit;
            if (currentBalance < command.Amount.Value)
            {
                return CommandResult.Fail(
                    $"Insufficient balance. Current: {currentBalance} gold, Requested: {command.Amount.Value} gold");
            }

            // Check cancellation before mutation
            cancellationToken.ThrowIfCancellationRequested();

            // Update account balance
            account.Debit -= command.Amount.Value;
            account.LastAccessedAt = DateTime.UtcNow;

            // Record transaction
            var transaction = new Transaction
            {
                FromPersonaId = coinhouse.PersonaId.ToString(),
                ToPersonaId = command.PersonaId.ToString(),
                Amount = command.Amount.Value,
                Memo = $"Withdrawal: {command.Reason.Value}",
                Timestamp = DateTime.UtcNow
            };

            var recordedTransaction = await _transactions.RecordTransactionAsync(
                transaction,
                cancellationToken);

            // Publish event
            var evt = new GoldWithdrawnEvent(
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

    private static Guid ExtractAccountId(SharedKernel.Personas.PersonaId personaId)
    {
        // PersonaId format: "Type:Value"
        // For Character personas, Value is the CharacterId Guid
        // For Organization personas, Value is the OrganizationId Guid
        var parts = personaId.ToString().Split(':');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid PersonaId format: {personaId}");
        }

        if (Guid.TryParse(parts[1], out var guid))
        {
            return guid;
        }

        // For non-Guid persona types (e.g., Coinhouse, System), generate deterministic Guid
        // This ensures consistent account IDs for the same persona
        return Guid.NewGuid(); // TODO: Implement deterministic Guid generation from string
    }
}

