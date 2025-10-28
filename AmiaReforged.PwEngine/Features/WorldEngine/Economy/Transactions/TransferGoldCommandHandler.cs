using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;

/// <summary>
/// Handles the execution of TransferGoldCommand.
/// Validates the transfer, records the transaction, publishes event, and returns the result.
/// Note: This handler does NOT manage actual gold/wallet balances - it only logs transfers.
/// Balance management is handled by wallet/coinhouse services.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<TransferGoldCommand>))]
public class TransferGoldCommandHandler : ICommandHandler<TransferGoldCommand>
{
    private readonly ITransactionRepository _repository;
    private readonly IEventBus _eventBus;

    public TransferGoldCommandHandler(ITransactionRepository repository, IEventBus eventBus)
    {
        _repository = repository;
        _eventBus = eventBus;
    }

    public async Task<CommandResult> HandleAsync(TransferGoldCommand command, CancellationToken cancellationToken = default)
    {
        // Validate command
        (bool isValid, string? errorMessage) = command.Validate();
        if (!isValid)
        {
            return CommandResult.Fail(errorMessage!);
        }

        try
        {
            // Create transaction entity
            Transaction transaction = new Transaction
            {
                FromPersonaId = command.From.ToString(),
                ToPersonaId = command.To.ToString(),
                Amount = command.Amount.Value,
                Memo = command.Memo,
                Timestamp = DateTime.UtcNow
            };

            // Record transaction
            Transaction recorded = await _repository.RecordTransactionAsync(transaction, cancellationToken);

            // Publish event
            var evt = new GoldTransferredEvent(
                command.From,
                command.To,
                command.Amount,
                TransactionId.NewId(), // TODO: Use DB transaction ID once we switch to Guid
                command.Memo,
                recorded.Timestamp);
            await _eventBus.PublishAsync(evt, cancellationToken);

            // Return success with transaction ID
            return CommandResult.OkWith("transactionId", recorded.Id);
        }
        catch (Exception ex)
        {
            return CommandResult.Fail($"Failed to record transaction: {ex.Message}");
        }
    }
}

