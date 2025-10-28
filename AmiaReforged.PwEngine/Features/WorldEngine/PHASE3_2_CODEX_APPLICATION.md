# Phase 3.2: Codex Application Layer

**Status**: ✅ Complete
**Completion Date**: October 28, 2025

---

## Goal

Refactor the Codex subsystem to use the CQRS pattern established in Phase 3.1. Apply commands for all write operations and queries for read operations. Fix state management bugs and establish testing patterns for other subsystems to follow.

---

## Summary

Phase 3.2 successfully applied CQRS to the Codex subsystem, serving as the reference implementation for other subsystems. All Codex operations now use commands/queries, state management bugs were fixed, and comprehensive test coverage was achieved.

---

## Accomplishments

### Commands Implemented
- `DiscoverQuestCommand` - Mark quest as discovered
- `ActivateQuestCommand` - Start active tracking
- `CompleteQuestCommand` - Mark quest complete
- `FailQuestCommand` - Mark quest failed
- `UpdateQuestProgressCommand` - Update quest progress
- `AddCodexNoteCommand` - Add note entry
- `AddCodexLoreCommand` - Add lore entry
- (Additional commands as needed)

### Queries Implemented
- `GetActiveQuestsQuery` - Retrieve active quests
- `GetCompletedQuestsQuery` - Retrieve completed quests
- `GetCodexNotesQuery` - Retrieve notes
- `GetCodexLoreQuery` - Retrieve lore entries
- `GetQuestDetailsQuery` - Get detailed quest information

### State Management Fixes
- Fixed "Discovered" state handling for quests
- Fixed state transition validation
- Fixed progress tracking consistency
- Fixed state machine edge cases

See dedicated documents:
- `QUEST_DISCOVERED_STATE_FIX.md`
- `QUEST_STATE_CONSISTENCY_FIX.md`
- `QUEST_STATE_FIX.md`
- `QUEST_TEST_FIX_COMPLETE.md`

---

## Files Created/Modified

### Commands
- Multiple command files in `Features/WorldEngine/Codex/Application/Commands/`
- Command handler implementations

### Queries
- Multiple query files in `Features/WorldEngine/Codex/Application/Queries/`
- Query handler implementations

### Aggregates
- Updated `PlayerCodex` aggregate
- Fixed state machine in quest entries
- Added proper validation

### Tests
- Comprehensive BDD-style tests for all commands/queries
- State transition tests
- Integration tests
- **All tests passing** ✅

---

## Key Achievements

### 1. Reference Implementation
Phase 3.2 established the pattern for other subsystems:
- BDD test-first approach
- Command/query separation
- State management best practices
- Error handling patterns

### 2. Bug Fixes
Multiple state management bugs discovered and fixed:
- Quest "Discovered" state not properly tracked
- Invalid state transitions allowed
- Progress updates inconsistent with state
- Missing validation in aggregate

### 3. Test Coverage
Comprehensive test suite provides confidence:
- Unit tests for commands/queries
- Aggregate state tests
- Integration tests
- Edge case coverage

### 4. Documentation
Complete documentation of:
- State machine behavior
- Command/query usage
- Bug fixes applied
- Migration patterns

---

## Lessons Learned

### 1. State Machines Need Explicit Tests
**Discovery**: State transition bugs only caught by explicit state machine tests
**Solution**: Add dedicated state transition test suite for each aggregate

### 2. BDD Tests Catch Edge Cases
**Discovery**: Given-When-Then tests revealed edge cases not in requirements
**Solution**: Write tests first to explore behavior space

### 3. Aggregates Own State Logic
**Discovery**: Commands shouldn't contain state validation logic
**Solution**: Move all state transitions into aggregate methods

### 4. Integration Tests Are Essential
**Discovery**: Unit tests alone missed cross-component issues
**Solution**: Add integration tests for end-to-end scenarios

---

## Migration Pattern for Other Subsystems

### Step 1: Identify Operations
List all write operations (commands) and read operations (queries) in the subsystem.

### Step 2: Create Commands First
Write BDD tests for each command, then implement handlers.

### Step 3: Create Queries
Write tests for queries, implement handlers (simpler than commands).

### Step 4: Update Aggregates
Fix any state management issues discovered during testing.

### Step 5: Integration Tests
Add end-to-end tests for complete workflows.

---

## Success Criteria

- [x] All Codex operations use commands/queries
- [x] State management bugs fixed
- [x] Comprehensive test coverage
- [x] All tests passing
- [x] Documentation complete
- [x] Pattern established for other subsystems

---

## Related Documents

### Completion Documents
- `PHASE3_PART2_COMPLETE.md`
- `PHASE3_PART2_ALL_FIXES_COMPLETE.md`

### Bug Fix Documents
- `QUEST_DISCOVERED_STATE_FIX.md`
- `QUEST_STATE_CONSISTENCY_FIX.md`
- `QUEST_STATE_FIX.md`
- `QUEST_TEST_FIX_COMPLETE.md`

---

**Completion Date**: October 28, 2025
**Previous Phase**: [Phase 3.1: CQRS Infrastructure](PHASE3_1_CQRS_INFRASTRUCTURE.md)
**Next Phase**: [Phase 3.3: Economy Expansion](PHASE3_3_ECONOMY_EXPANSION.md)
# Phase 3.1: CQRS Infrastructure

**Status**: ✅ Complete
**Completion Date**: October 2025

---

## Goal

Establish clear API boundaries between command execution and data queries. Create infrastructure for CQRS pattern implementation across all WorldEngine subsystems.

---

## What is CQRS?

**Command Query Responsibility Segregation** separates:
- **Commands**: Change state, return success/failure
- **Queries**: Read data, return results, no side effects

This enables:
- Clear separation of concerns
- Easier testing (mock commands vs queries)
- Event sourcing preparation
- Performance optimization (different read/write models)

---

## Infrastructure Created

### Command Pattern

```csharp
// Base interfaces
public interface ICommand { }

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}

// Result type
public readonly record struct CommandResult(
    bool Success,
    string? ErrorMessage = null,
    Dictionary<string, object>? Data = null)
{
    public static CommandResult Ok() => new(true);

    public static CommandResult OkWith(string key, object value) =>
        new(true, Data: new Dictionary<string, object> { [key] = value });

    public static CommandResult Fail(string error) =>
        new(false, error);
}
```

### Query Pattern

```csharp
// Base interfaces
public interface IQuery<TResult> { }

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken ct = default);
}
```

### Service Binding

Handlers registered via Anvil's service binding:

```csharp
[ServiceBinding(typeof(ICommandHandler<MyCommand>))]
public sealed class MyCommandHandler : ICommandHandler<MyCommand>
{
    public async Task<CommandResult> HandleAsync(
        MyCommand command,
        CancellationToken ct)
    {
        // Validate, execute, return result
        return CommandResult.Ok();
    }
}
```

---

## Directory Structure

```
Features/WorldEngine/
  Commands/
    Economy/
      TransferGoldCommand.cs
      DepositToCoinhouseCommand.cs
    Industries/
      StartProductionCommand.cs
    Organizations/
      GrantReputationCommand.cs

  Queries/
    Economy/
      GetCoinhouseBalanceQuery.cs
      GetPersonaTransactionHistoryQuery.cs
    Industries/
      GetActiveProductionQuery.cs
    Regions/
      GetSettlementsInRegionQuery.cs

  Events/ (Phase 4)
    Economy/
      GoldTransferredEvent.cs
    Industries/
      ProductionStartedEvent.cs
```

---

## Example: Command Implementation

### Command Definition
```csharp
public sealed record TransferGoldCommand(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    string Reason) : ICommand
{
    // Factory method with validation
    public static TransferGoldCommand Create(
        PersonaId from,
        PersonaId to,
        int amount,
        string reason)
    {
        if (from == to)
            throw new InvalidOperationException("Cannot transfer to self");

        return new TransferGoldCommand(
            from,
            to,
            Quantity.Parse(amount), // Validates >= 0
            reason);
    }
}
```

### Handler Implementation
```csharp
[ServiceBinding(typeof(ICommandHandler<TransferGoldCommand>))]
public sealed class TransferGoldCommandHandler
    : ICommandHandler<TransferGoldCommand>
{
    private readonly IPersonaRepository _personas;
    private readonly ITransactionRepository _transactions;

    public async Task<CommandResult> HandleAsync(
        TransferGoldCommand cmd,
        CancellationToken ct)
    {
        // Validate sender has funds
        var sender = await _personas.GetAsync(cmd.From, ct);
        if (sender.Gold < cmd.Amount)
            return CommandResult.Fail("Insufficient funds");

        // Execute transfer
        sender.Gold = sender.Gold.Subtract(cmd.Amount);
        var receiver = await _personas.GetAsync(cmd.To, ct);
        receiver.Gold = receiver.Gold.Add(cmd.Amount);

        // Record transaction
        await _transactions.RecordAsync(cmd, ct);

        // Return success
        return CommandResult.OkWith("transactionId", transaction.Id);
    }
}
```

---

## Example: Query Implementation

### Query Definition
```csharp
public sealed record GetCoinhouseBalanceQuery(
    CoinhouseTag Tag) : IQuery<Quantity>;
```

### Handler Implementation
```csharp
[ServiceBinding(typeof(IQueryHandler<GetCoinhouseBalanceQuery, Quantity>))]
public sealed class GetCoinhouseBalanceQueryHandler
    : IQueryHandler<GetCoinhouseBalanceQuery, Quantity>
{
    private readonly ICoinhouseRepository _coinhouses;

    public async Task<Quantity> HandleAsync(
        GetCoinhouseBalanceQuery query,
        CancellationToken ct)
    {
        var coinhouse = await _coinhouses.GetByTagAsync(query.Tag, ct);
        return coinhouse?.Balance ?? Quantity.Zero;
    }
}
```

---

## Migration Strategy

### Step 1: Create Infrastructure
- Added `ICommand`, `ICommandHandler<TCommand>` to SharedKernel
- Added `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>` to SharedKernel
- Created `CommandResult` type for command responses

### Step 2: Create Directory Structure
- Created `Commands/` and `Queries/` directories
- Organized by subsystem (Economy, Industries, etc.)

### Step 3: Refactor One Service Method at a Time
- Extract service method logic into command handler
- Service method becomes thin wrapper calling handler
- Gradually remove old service methods as handlers are implemented

### Step 4: Update Tests
- Test handlers directly, not service wrappers
- Use BDD Given-When-Then pattern
- Mock dependencies as needed

---

## Files Created

### Infrastructure
- `SharedKernel/Commands/ICommand.cs`
- `SharedKernel/Commands/ICommandHandler.cs`
- `SharedKernel/Commands/CommandResult.cs`
- `SharedKernel/Queries/IQuery.cs`
- `SharedKernel/Queries/IQueryHandler.cs`

### Directories
- `Features/WorldEngine/Commands/`
- `Features/WorldEngine/Queries/`
- `Features/WorldEngine/Events/` (prepared for Phase 4)

---

## Design Decisions

### 1. Command Validation
**Decision**: Validate in factory methods, not handlers
- Factory methods enforce business rules
- Value objects validate on construction
- Handlers assume valid input (fail-fast if not)
- No separate `IValidator<TCommand>` pipeline

**Rationale**:
- Simpler architecture
- Validation happens at creation time
- Clear error messages from factory methods

### 2. CommandResult Pattern
**Decision**: Use result type instead of exceptions
- `CommandResult.Ok()` for success
- `CommandResult.Fail(error)` for business rule failures
- Exceptions only for unexpected errors

**Rationale**:
- Easier to test
- Clear success/failure semantics
- Can return data with `OkWith(key, value)`

### 3. Query Return Types
**Decision**: Return primitives/DTOs, never domain entities
- Queries return simple types (`Quantity`, DTOs)
- No change tracking on query results
- Clear read-only semantics

**Rationale**:
- Prevents accidental mutations
- Better performance (no EF tracking)
- Clear separation from commands

### 4. Handler Registration
**Decision**: Use Anvil's `[ServiceBinding]` attribute
- Automatic DI registration
- Type-safe handler resolution
- Follows existing patterns in codebase

---

## Testing Patterns

### Command Handler Tests
```csharp
[Test]
public async Task Given_ValidCommand_When_Executed_Then_ReturnsSuccess()
{
    // Given
    var command = TransferGoldCommand.Create(from, to, 100, "Test");

    // When
    var result = await _handler.HandleAsync(command);

    // Then
    Assert.That(result.Success, Is.True);
}
```

### Query Handler Tests
```csharp
[Test]
public async Task Given_ExistingCoinhouse_When_Querying_Then_ReturnsBalance()
{
    // Given
    var query = new GetCoinhouseBalanceQuery(tag);
    _mockRepo.Setup(r => r.GetByTagAsync(tag, default))
        .ReturnsAsync(testCoinhouse);

    // When
    var balance = await _handler.HandleAsync(query);

    // Then
    Assert.That(balance, Is.EqualTo(expectedBalance));
}
```

---

## Success Criteria

- [x] Command/query interfaces created
- [x] Handler base pattern established
- [x] Directory structure organized
- [x] Service binding working
- [x] Example implementations created
- [x] Test patterns documented
- [x] Ready for subsystem implementation

---

## Impact on Future Phases

This infrastructure enables:
- **Phase 3.2+**: Subsystem-specific handlers (Codex, Economy, etc.)
- **Phase 4**: Event publishing from command handlers
- **Phase 5**: Unified `IWorldEngine` façade wrapping handlers

---

**Completion Date**: October 2025
**Previous Phase**: [Phase 2: Persona Abstraction](PHASE2_PERSONA_ABSTRACTION.md)
**Next Phase**: [Phase 3.2: Codex Application](PHASE3_2_CODEX_APPLICATION.md)

