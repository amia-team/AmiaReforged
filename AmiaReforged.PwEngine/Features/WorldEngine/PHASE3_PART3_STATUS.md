# Phase 3.3: Economy CQRS Expansion - Current Status

**Date**: October 28, 2025
**Status**: 🟢 In Progress - Foundation Complete, Handlers Next

---

## Overview

Phase 3.3 is applying the CQRS pattern to the Economy subsystem. We've established a solid foundation with value objects, test helpers, and our first command. Now we need to implement command handlers and queries to complete the economy expansion.

---

## ✅ Completed Work

### 1. Foundation Infrastructure ✅

**Value Objects** (3 created):
- `GoldAmount` - Non-negative gold quantities with arithmetic operations
- `TransactionReason` - Validated reason strings (3-200 chars)
- `TransactionId` - GUID-based unique transaction identifiers

**Test Helpers**:
- `EconomyTestHelpers.cs` - Factory methods for common test objects
- `PersonaTestHelpers.cs` - Already existed, used extensively

**Test Infrastructure**:
- BDD Given-When-Then pattern established
- FluentAssertions integrated
- 81 passing Economy tests

### 2. Commands ✅

**Implemented**:
1. `TransferGoldCommand` - Transfer gold between personas
   - ✅ Command class with validation
   - ✅ Handler implementation
   - ✅ 18 passing tests
   - ✅ Repository integration

2. `DepositGoldCommand` - Deposit gold to coinhouse
   - ✅ Command class with factory method
   - ✅ 11 passing tests
   - ⚠️ **Handler NOT implemented yet**

### 3. Queries ✅

**Implemented**:
1. `GetTransactionHistoryQuery` - Get transaction history for a persona
   - ✅ Query class
   - ✅ Handler implementation
   - ✅ Repository support (multiple query methods)

### 4. Repositories ✅

**TransactionRepository** (In-memory implementation):
- ✅ `RecordTransactionAsync`
- ✅ `GetByIdAsync`
- ✅ `GetHistoryAsync` (with pagination)
- ✅ `GetIncomingAsync`
- ✅ `GetOutgoingAsync`
- ✅ `GetBetweenPersonasAsync`
- ✅ `GetTotalSentAsync`
- ✅ `GetTotalReceivedAsync`
- ✅ 48 passing repository tests

### 5. Database Entities ✅

**Transaction Entity**:
- ✅ PersonaId support (From/To)
- ✅ Amount, Memo, Timestamp
- ✅ Proper conversions
- ✅ 15 entity tests

---

## ⏳ Remaining Work (Priority Order)

### Priority 1: Complete Deposit/Withdraw Handlers (Day 2-3)

**Tasks**:
1. ✅ `DepositGoldCommand` exists, needs handler:
   - Create `DepositGoldCommandHandler`
   - Write BDD tests for handler
   - Integrate with CoinhouseRepository
   - Publish `GoldDepositedEvent` (create event)

2. Create `WithdrawGoldCommand`:
   - Command class with factory method
   - Write BDD tests
   - Create handler
   - Balance validation logic
   - Publish `GoldWithdrawnEvent`

**Files to Create**:
- `Features/WorldEngine/Economy/Commands/DepositGoldCommandHandler.cs`
- `Features/WorldEngine/Economy/Commands/WithdrawGoldCommand.cs`
- `Features/WorldEngine/Economy/Commands/WithdrawGoldCommandHandler.cs`
- `Features/WorldEngine/Economy/Events/GoldDepositedEvent.cs`
- `Features/WorldEngine/Economy/Events/GoldWithdrawnEvent.cs`
- `Tests/Systems/WorldEngine/Economy/Commands/DepositGoldCommandHandlerTests.cs`
- `Tests/Systems/WorldEngine/Economy/Commands/WithdrawGoldCommandTests.cs`
- `Tests/Systems/WorldEngine/Economy/Commands/WithdrawGoldCommandHandlerTests.cs`

### Priority 2: Balance Queries (Day 3-4)

**Tasks**:
1. Create `GetBalanceQuery`:
   - Query for persona balance at a coinhouse
   - Handler implementation
   - BDD tests
   - DTO for results

2. Create `GetCoinhouseBalances Query`:
   - Query all balances for a persona
   - Handler implementation
   - BDD tests

**Files to Create**:
- `Features/WorldEngine/Economy/Queries/GetBalanceQuery.cs`
- `Features/WorldEngine/Economy/Queries/GetBalanceQueryHandler.cs`
- `Features/WorldEngine/Economy/Queries/GetCoinhouseBalancesQuery.cs`
- `Features/WorldEngine/Economy/Queries/GetCoinhouseBalancesQueryHandler.cs`
- `Features/WorldEngine/Economy/DTOs/BalanceDto.cs`
- `Features/WorldEngine/Economy/DTOs/CoinhouseBalancesDto.cs`
- `Tests/Systems/WorldEngine/Economy/Queries/GetBalanceQueryTests.cs`
- `Tests/Systems/WorldEngine/Economy/Queries/GetCoinhouseBalancesQueryTests.cs`

### Priority 3: Coinhouse Operations (Day 4)

**Tasks**:
1. Update `CoinhouseService` to use commands:
   - Refactor existing methods to use command handlers
   - Ensure no direct repository access from services
   - Add event publishing

2. Create coinhouse repository if needed:
   - Balance tracking
   - Limits enforcement
   - Settlement association

**Files to Update/Create**:
- `Features/WorldEngine/Economy/Banks/CoinhouseService.cs` (refactor)
- `Features/WorldEngine/Economy/Banks/ICoinhouseRepository.cs` (may need creation)
- `Features/WorldEngine/Economy/Banks/CoinhouseRepository.cs` (may need creation)

### Priority 4: Events and Event Bus (Day 5)

**Tasks**:
1. Create domain events:
   - `GoldDepositedEvent`
   - `GoldWithdrawnEvent`
   - `GoldTransferredEvent` (already have command, add event)

2. Update handlers to publish events:
   - All command handlers must publish events
   - Add event assertions to tests

3. Event bus integration:
   - Verify `IEventBus` exists from Phase 4 prep
   - If not, create stub/in-memory implementation
   - Wire up event subscribers

**Files to Create**:
- `Features/WorldEngine/Economy/Events/GoldDepositedEvent.cs`
- `Features/WorldEngine/Economy/Events/GoldWithdrawnEvent.cs`
- `Features/WorldEngine/Economy/Events/GoldTransferredEvent.cs`

### Priority 5: Integration Tests (Day 5)

**Tasks**:
1. End-to-end scenarios:
   - Character deposits → withdraw → transfer flow
   - Organization coinhouse operations
   - Cross-persona transactions
   - Balance query integration

2. Event verification:
   - Events published in correct order
   - Event data accuracy
   - Event subscribers react correctly

**Files to Create**:
- `Tests/Systems/WorldEngine/Economy/Integration/EconomyIntegrationTests.cs`
- `Tests/Systems/WorldEngine/Economy/Integration/EventPublishingTests.cs`

### Priority 6: Documentation (Day 5)

**Tasks**:
1. Update refactoring plan:
   - Mark Phase 3.3 as complete
   - Document lessons learned
   - Update success criteria

2. Create API examples:
   - Usage patterns
   - Common scenarios
   - Error handling

**Files to Update/Create**:
- `Features/WorldEngine/Refactoring.md` (update status)
- `Features/WorldEngine/Economy/README.md` (create usage guide)
- `Features/WorldEngine/PHASE3_PART3_COMPLETE.md` (completion doc)

---

## Architecture Decisions

### 1. Command Validation Strategy
**Decision**: Validate in factory methods, not handlers
- Value objects validate on construction (e.g., `GoldAmount.Parse`)
- Command factory methods enforce business rules
- Handlers assume valid input and focus on orchestration

### 2. Event Publishing
**Decision**: Handlers publish events after successful persistence
- Events represent "what happened" (past tense)
- Published after database commit
- Event data is immutable snapshot

### 3. Repository Pattern
**Decision**: In-memory for now, DB-backed later
- `ITransactionRepository` already in-memory
- Easy to swap for EF Core implementation
- Tests run fast without DB dependencies

### 4. Query DTOs
**Decision**: Never return domain entities from queries
- DTOs are simple, immutable data structures
- No behavior, no business logic
- Safe to pass across boundaries

---

## Test Coverage Status

**Current**: 81 tests passing
- ✅ Commands: 29 tests
- ✅ Handlers: 10 tests
- ✅ Repositories: 15 tests
- ✅ Entities: 15 tests
- ✅ Value Objects: 11 tests (implied from command tests)
- ✅ Misc: 1 test

**Target**: 150+ tests
- Commands: ~60 tests (all CRUD operations)
- Handlers: ~40 tests (orchestration logic)
- Queries: ~20 tests (all query types)
- Integration: ~20 tests (end-to-end scenarios)
- Events: ~10 tests (publishing verification)

---

## Blockers & Dependencies

### Potential Blockers
1. ⚠️ **Event Bus** - May need to create if Phase 4 not started
   - **Resolution**: Create simple in-memory `IEventBus` stub
   - No async processing needed yet
   - Synchronous event dispatch is fine for Phase 3.3

2. ⚠️ **Coinhouse Repository** - Doesn't exist yet
   - **Resolution**: Create interface + in-memory implementation
   - Similar to `TransactionRepository` pattern
   - Track balances in `Dictionary<(PersonaId, CoinhouseTag), int>`

3. ⚠️ **CoinhouseTag to RegionTag resolution** - Already exists!
   - ✅ `RegionPolicyResolver` has this logic
   - ✅ Tests passing (RegionPolicyResolverBehaviorTests)

### No Blockers
- ✅ PersonaId infrastructure exists
- ✅ Value objects pattern established
- ✅ Test helpers created
- ✅ Command/Query infrastructure from Phase 3.1

---

## Next Immediate Steps

### Step 1: Create Event Stubs (15 minutes)
Create the three event classes so handlers can reference them:
```csharp
public sealed record GoldDepositedEvent(
    PersonaId PersonaId,
    CoinhouseTag Coinhouse,
    GoldAmount Amount,
    TransactionId TransactionId,
    DateTime OccurredAt) : IDomainEvent;

public sealed record GoldWithdrawnEvent(...);
public sealed record GoldTransferredEvent(...);
```

### Step 2: Create IEventBus Stub (30 minutes)
If `IEventBus` doesn't exist, create minimal version:
```csharp
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent;
}

// In-memory stub (synchronous)
[ServiceBinding(typeof(IEventBus))]
public class InMemoryEventBus : IEventBus
{
    private readonly List<IDomainEvent> _publishedEvents = new();

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        _publishedEvents.Add(@event);
        return Task.CompletedTask;
    }

    // For testing
    public IReadOnlyList<IDomainEvent> PublishedEvents => _publishedEvents;
}
```

### Step 3: Implement DepositGoldCommandHandler (2 hours)
- Write BDD tests first
- Create handler
- Integrate with CoinhouseRepository
- Publish event
- All tests green

### Step 4: Implement WithdrawGoldCommand + Handler (3 hours)
- Command with factory method + tests
- Handler with balance validation + tests
- Event publishing
- All tests green

### Step 5: Continue with remaining priorities...

---

## Success Criteria (Phase 3.3)

- [ ] All Economy write operations use command handlers
- [ ] All Economy read operations use query handlers
- [ ] No direct repository access from services
- [ ] All mutations publish domain events
- [ ] 150+ tests passing with >90% coverage
- [ ] BDD pattern used consistently
- [ ] Integration tests cover end-to-end scenarios
- [ ] Documentation complete with examples
- [ ] Zero compilation warnings in Economy namespace

---

## Timeline Estimate

- **Day 1**: ✅ Complete (foundation, value objects, test helpers, TransferGoldCommand)
- **Day 2**: ⏳ Next - Complete deposit/withdraw handlers
- **Day 3**: Finish commands, start queries
- **Day 4**: Complete queries, refactor services
- **Day 5**: Integration tests, events, documentation

**Total**: 5 days (Week 5-6 per Refactoring.md timeline)

---

## Files Created So Far

### Features
1. `Features/WorldEngine/Economy/ValueObjects/GoldAmount.cs`
2. `Features/WorldEngine/Economy/ValueObjects/TransactionReason.cs`
3. `Features/WorldEngine/Economy/ValueObjects/TransactionId.cs`
4. `Features/WorldEngine/Economy/Commands/DepositGoldCommand.cs`
5. `Features/WorldEngine/Economy/Transactions/TransferGoldCommand.cs`
6. `Features/WorldEngine/Economy/Transactions/TransferGoldCommandHandler.cs`
7. `Features/WorldEngine/Economy/Transactions/GetTransactionHistoryQuery.cs`
8. `Features/WorldEngine/Economy/Transactions/GetTransactionHistoryQueryHandler.cs`
9. `Features/WorldEngine/Economy/Transactions/ITransactionRepository.cs`
10. `Features/WorldEngine/Economy/Transactions/TransactionRepository.cs`

### Tests
1. `Tests/Helpers/WorldEngine/EconomyTestHelpers.cs`
2. `Tests/Systems/WorldEngine/Economy/Commands/DepositGoldCommandTests.cs`
3. `Tests/Systems/WorldEngine/Economy/TransferGoldCommandTests.cs`
4. `Tests/Systems/WorldEngine/Economy/TransferGoldCommandHandlerTests.cs`
5. `Tests/Systems/WorldEngine/Economy/TransactionEntityTests.cs`
6. `Tests/Systems/WorldEngine/Economy/TransactionRepositoryTests.cs`
7. `Tests/Systems/WorldEngine/Economy/RegionPolicyResolverTests.cs`
8. `Tests/Systems/WorldEngine/Economy/RegionPolicyResolverBehaviorTests.cs`

### Documentation
1. `Features/WorldEngine/PHASE3_PART3_PLAN.md`
2. `Features/WorldEngine/PHASE3_PART3_DAY1_COMPLETE.md`
3. `Features/WorldEngine/TRANSACTION_REPOSITORY_INMEMORY_COMPLETE.md`

---

## Let's Continue! 🚀

**Current focus**: Implement the command handlers and complete the deposit/withdraw operations.

**Next action**: Create event stubs and implement `DepositGoldCommandHandler`.

