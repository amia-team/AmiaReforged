using System;
using System.Collections.Generic;
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
            CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(command.Coinhouse, cancellationToken);
            if (coinhouse is null)
            {
                return CommandResult.Fail($"Coinhouse '{command.Coinhouse.Value}' not found");
            }

            cancellationToken.ThrowIfCancellationRequested();

            Guid accountId = PersonaAccountId.ForCoinhouse(command.PersonaId, command.Coinhouse);
            CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

            DateTime timestamp = DateTime.UtcNow;
            CoinhouseAccountDto updatedAccount = account is null
                ? CreateNewAccount(accountId, coinhouse, command.PersonaId, timestamp)
                : account with { LastAccessedAt = timestamp };

            updatedAccount = updatedAccount with
            {
                Debit = updatedAccount.Debit + command.Amount.Value,
                LastAccessedAt = timestamp
            };

            await _coinhouses.SaveAccountAsync(updatedAccount, cancellationToken);

            Transaction transaction = new Transaction
            {
                FromPersonaId = command.PersonaId.ToString(),
                ToPersonaId = coinhouse.Persona.ToString(),
                Amount = command.Amount.Value,
                Memo = $"Deposit: {command.Reason.Value}",
                Timestamp = timestamp
            };

            Transaction recordedTransaction = await _transactions.RecordTransactionAsync(transaction, cancellationToken);

            GoldDepositedEvent evt = new GoldDepositedEvent(
                command.PersonaId,
                command.Coinhouse,
                command.Amount,
                TransactionId.NewId(),
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

    private static CoinhouseAccountDto CreateNewAccount(
        Guid accountId,
        CoinhouseDto coinhouse,
        PersonaId owner,
        DateTime timestamp)
    {
        return new CoinhouseAccountDto
        {
            Id = accountId,
            Debit = 0,
            Credit = 0,
            CoinHouseId = coinhouse.Id,
            OpenedAt = timestamp,
            LastAccessedAt = timestamp,
            Coinhouse = coinhouse,
            Holders = CreateDefaultHolders(owner)
        };
    }

    private static IReadOnlyList<CoinhouseAccountHolderDto> CreateDefaultHolders(PersonaId owner)
    {
        if (!Guid.TryParse(owner.Value, out Guid holderId))
        {
            return Array.Empty<CoinhouseAccountHolderDto>();
        }

        HolderType holderType = owner.Type switch
        {
            PersonaType.Organization => HolderType.Organization,
            PersonaType.Government => HolderType.Government,
            _ => HolderType.Individual
        };

        CoinhouseAccountHolderDto primaryHolder = new()
        {
            HolderId = holderId,
            Type = holderType,
            Role = HolderRole.Owner,
            FirstName = owner.Value,
            LastName = string.Empty
        };

        return new[] { primaryHolder };
    }
}

