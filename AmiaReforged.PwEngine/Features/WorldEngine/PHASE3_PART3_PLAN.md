# Phase 3.3: Economy CQRS Expansion - Implementation Plan

**Date**: October 28, 2025
**Status**: 🚀 In Progress
**Approach**: BDD Test-First, DDD Principles, Ergonomic API Design

---

## Overview

Expand the CQRS pattern to the Economy subsystem, introducing commands and queries for gold management, transactions, and coinhouse operations. Focus on creating an intuitive, type-safe API that prevents common errors at compile time.

---

## Goals

1. **Commands**: Encapsulate all write operations with validation and events
2. **Queries**: Provide read-only access to economic data
3. **Events**: Publish domain events for all state changes
4. **Type Safety**: Use value objects to prevent invalid operations
5. **Ergonomics**: Make the happy path easy and the wrong path hard

---

## Scope - Phase 3.3

### Commands (Write Operations)

1. **Gold Management**
   - `DepositGoldCommand` - Deposit gold to a coinhouse
   - `WithdrawGoldCommand` - Withdraw gold from a coinhouse
   - `TransferGoldCommand` - Transfer gold between personas

2. **Transaction Management**
   - `RecordTransactionCommand` - Record a transaction for audit
   - `ReverseTransactionCommand` - Reverse a transaction (refunds)

3. **Coinhouse Operations**
   - `CreateCoinhouseCommand` - Create a new coinhouse
   - `UpdateCoinhouseLimitsCommand` - Update deposit/withdrawal limits

### Queries (Read Operations)

1. **Balance Queries**
   - `GetBalanceQuery` - Get persona's balance at a coinhouse
   - `GetTotalWealthQuery` - Get total wealth across all coinhouses

2. **Transaction Queries**
   - `GetTransactionHistoryQuery` - Get transaction history for a persona
   - `GetTransactionDetailsQuery` - Get details of a specific transaction

3. **Coinhouse Queries**
   - `GetCoinhouseInfoQuery` - Get coinhouse information
   - `GetAllCoinhousesQuery` - List all coinhouses in a settlement

### Events (Domain Events)

1. **Gold Events**
   - `GoldDepositedEvent` - Gold was deposited
   - `GoldWithdrawnEvent` - Gold was withdrawn
   - `GoldTransferredEvent` - Gold was transferred between personas

2. **Transaction Events**
   - `TransactionRecordedEvent` - Transaction was recorded
   - `TransactionReversedEvent` - Transaction was reversed

---

## BDD Test-First Approach

### Test Structure

Each feature follows the Given-When-Then pattern:

```csharp
[Test]
public async Task Given_ValidPersonaWithSufficientFunds_When_WithdrawingGold_Then_BalanceDecreasesAndEventPublished()
{
    // Given - Arrange the world state
    var persona = PersonaTestHelpers.CreateCharacterPersona();
    var coinhouse = CoinhouseTag.Parse("cordor_bank");
    await SetupPersonaBalance(persona.Id, coinhouse, 1000);

    // When - Execute the command
    var command = WithdrawGoldCommand.Create(persona.Id, coinhouse, 500, "Buying supplies");
    var result = await _handler.HandleAsync(command);

    // Then - Assert the outcome
    result.Should().BeSuccess();

    var balance = await GetBalance(persona.Id, coinhouse);
    balance.Should().Be(500);

    _eventBus.Should().HavePublished<GoldWithdrawnEvent>(e =>
        e.PersonaId == persona.Id &&
        e.Amount.Value == 500);
}
```

### Test Categories

1. **Happy Path Tests** - Normal successful operations
2. **Validation Tests** - Invalid input handling
3. **Business Rule Tests** - Domain rules enforcement
4. **Event Tests** - Event publication verification
5. **Integration Tests** - End-to-end scenarios

---

## Implementation Strategy

### Step 1: Value Objects and Domain Types

Create the building blocks first:
- `GoldAmount` - Validated gold quantity
- `TransactionId` - Unique transaction identifier
- `TransactionType` - Enum for transaction categories
- `TransactionReason` - Validated reason string

### Step 2: Commands and Results

Commands are immutable records with factory methods:
```csharp
public sealed record DepositGoldCommand
{
    public required PersonaId PersonaId { get; init; }
    public required CoinhouseTag Coinhouse { get; init; }
    public required GoldAmount Amount { get; init; }
    public required string Reason { get; init; }

    public static DepositGoldCommand Create(
        PersonaId personaId,
        CoinhouseTag coinhouse,
        int amount,
        string reason)
    {
        return new DepositGoldCommand
        {
            PersonaId = personaId,
            Coinhouse = coinhouse,
            Amount = GoldAmount.Parse(amount),
            Reason = TransactionReason.Validate(reason)
        };
    }
}
```

### Step 3: Queries and DTOs

Queries return DTOs, not domain entities:
```csharp
public sealed record GetBalanceQuery(PersonaId PersonaId, CoinhouseTag Coinhouse);

public sealed record BalanceDto(
    PersonaId PersonaId,
    CoinhouseTag Coinhouse,
    int CurrentBalance,
    DateTime LastUpdated);
```

### Step 4: Events

Events are immutable records capturing what happened:
```csharp
public sealed record GoldDepositedEvent(
    PersonaId PersonaId,
    CoinhouseTag Coinhouse,
    GoldAmount Amount,
    TransactionId TransactionId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
```

### Step 5: Handlers

Handlers orchestrate the operation:
```csharp
public class DepositGoldCommandHandler : ICommandHandler<DepositGoldCommand>
{
    private readonly ICoinhouseRepository _coinhouses;
    private readonly ITransactionRepository _transactions;
    private readonly IEventBus _eventBus;

    public async Task<CommandResult> HandleAsync(
        DepositGoldCommand command,
        CancellationToken ct = default)
    {
        // 1. Load aggregate
        var coinhouse = await _coinhouses.GetByTagAsync(command.Coinhouse, ct);
        if (coinhouse == null)
            return CommandResult.Failure($"Coinhouse {command.Coinhouse} not found");

        // 2. Execute business logic
        var transaction = coinhouse.DepositGold(
            command.PersonaId,
            command.Amount,
            command.Reason);

        // 3. Persist changes
        await _coinhouses.SaveAsync(coinhouse, ct);
        await _transactions.SaveAsync(transaction, ct);

        // 4. Publish event
        var evt = new GoldDepositedEvent(
            command.PersonaId,
            command.Coinhouse,
            command.Amount,
            transaction.Id,
            DateTime.UtcNow);
        await _eventBus.PublishAsync(evt, ct);

        return CommandResult.Success();
    }
}
```

---

## Ergonomic Design Principles

### 1. Make Invalid States Unrepresentable

```csharp
// ❌ Bad - Can create negative amounts
public record DepositCommand(PersonaId PersonaId, int Amount);

// ✅ Good - GoldAmount validates >= 0
public record DepositCommand(PersonaId PersonaId, GoldAmount Amount);
```

### 2. Use Factory Methods for Validation

```csharp
// ❌ Bad - Can forget validation
var command = new DepositGoldCommand { Amount = -100 };

// ✅ Good - Factory enforces rules
var command = DepositGoldCommand.Create(personaId, coinhouse, -100, reason);
// Throws ArgumentException
```

### 3. Descriptive Error Messages

```csharp
// ❌ Bad
return CommandResult.Failure("Invalid");

// ✅ Good
return CommandResult.Failure(
    $"Cannot withdraw {amount} gold from {coinhouse}. " +
    $"Current balance: {currentBalance}");
```

### 4. Fluent Assertions in Tests

```csharp
// ❌ Bad
Assert.That(result.Success, Is.True);
Assert.That(result.ErrorMessage, Is.Null);

// ✅ Good
result.Should().BeSuccess();
```

### 5. Event Testing Helpers

```csharp
// ✅ Create fluent event assertions
_eventBus.Should().HavePublished<GoldDepositedEvent>(e =>
    e.PersonaId == persona.Id &&
    e.Amount.Value == 500);
```

---

## File Structure

```
Features/WorldEngine/Economy/
  Commands/
    DepositGoldCommand.cs
    WithdrawGoldCommand.cs
    TransferGoldCommand.cs

  Queries/
    GetBalanceQuery.cs
    GetTransactionHistoryQuery.cs

  Handlers/
    Commands/
      DepositGoldCommandHandler.cs
      WithdrawGoldCommandHandler.cs
    Queries/
      GetBalanceQueryHandler.cs
      GetTransactionHistoryQueryHandler.cs

  Events/
    GoldDepositedEvent.cs
    GoldWithdrawnEvent.cs
    GoldTransferredEvent.cs

  ValueObjects/
    GoldAmount.cs
    TransactionReason.cs

  DTOs/
    BalanceDto.cs
    TransactionDto.cs

Tests/Systems/WorldEngine/Economy/
  Commands/
    DepositGoldCommandTests.cs
    WithdrawGoldCommandTests.cs

  Queries/
    GetBalanceQueryTests.cs

  Integration/
    EconomyIntegrationTests.cs

  Helpers/
    EconomyTestHelpers.cs
```

---

## Implementation Timeline

### Day 1: Foundation
- ✅ Create implementation plan (this file)
- ⏳ Create value objects (GoldAmount, TransactionReason)
- ⏳ Create test helpers
- ⏳ Set up test infrastructure

### Day 2: Deposit Command
- ⏳ Write BDD tests for DepositGoldCommand
- ⏳ Implement DepositGoldCommand and handler
- ⏳ Verify all tests pass

### Day 3: Withdrawal & Transfer
- ⏳ Write BDD tests for WithdrawGoldCommand
- ⏳ Implement WithdrawGoldCommand and handler
- ⏳ Write BDD tests for TransferGoldCommand
- ⏳ Implement TransferGoldCommand and handler

### Day 4: Queries
- ⏳ Write BDD tests for balance queries
- ⏳ Implement query handlers
- ⏳ Write integration tests

### Day 5: Polish & Documentation
- ⏳ Complete all tests
- ⏳ Add documentation
- ⏳ Update Refactoring.md

---

## Success Criteria

- [ ] All commands have comprehensive BDD tests
- [ ] All handlers publish appropriate events
- [ ] Queries return DTOs, never domain entities
- [ ] Value objects prevent invalid states
- [ ] Factory methods enforce business rules
- [ ] Integration tests cover end-to-end scenarios
- [ ] All tests use fluent assertions
- [ ] Code coverage > 90% for Economy subsystem
- [ ] Documentation includes usage examples
- [ ] Zero compilation warnings

---

## Next Steps

1. Create `GoldAmount` value object
2. Create `EconomyTestHelpers`
3. Write first BDD test for `DepositGoldCommand`
4. Implement to make test pass
5. Iterate

Let's start building! 🚀

