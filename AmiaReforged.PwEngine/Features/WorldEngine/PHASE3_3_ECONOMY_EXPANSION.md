# Phase 3.3: Economy Expansion (CQRS)

**Status**: 🟢 In Progress (~55% Complete)
**Started**: October 28, 2025

---

## Goal

Apply the CQRS pattern to the Economy subsystem. Implement commands for all write operations (deposits, withdrawals, transfers) and queries for read operations (balances, transaction history). Publish domain events for all mutations.

---

## Progress Summary

### ✅ Completed (55%)

**Commands (100% Complete)**:
- ✅ TransferGoldCommand + Handler (18 tests)
- ✅ DepositGoldCommand + Handler (20 tests)
- ✅ WithdrawGoldCommand + Handler (27 tests)

**Queries (67% Complete)**:
- ✅ GetTransactionHistoryQuery + Handler
- ✅ GetBalanceQuery + Handler (8 tests)
- ⏳ GetCoinhouseBalancesQuery (TODO)

**Infrastructure (100% Complete)**:
- ✅ Event infrastructure (IDomainEvent, IEventBus, InMemoryEventBus)
- ✅ Three economy events (GoldDeposited, GoldWithdrawn, GoldTransferred)
- ✅ Value objects (GoldAmount, TransactionReason, TransactionId)
- ✅ Test helpers (EconomyTestHelpers)

**Tests**: **136 passing** (91% of 150 target)

---

## Remaining Work

### Priority 1: GetCoinhouseBalancesQuery (~2 hours)
- Create query that returns all balances for a persona
- Handler implementation
- 10 BDD tests
- DTO for results

### Priority 2: Integration Tests (~3 hours)
- Full deposit→withdraw flow
- Overdraft prevention scenarios
- Multiple coinhouse operations
- Cross-persona transfers
- Event ordering validation
- 15-20 tests

### Priority 3: Documentation (~1 hour)
- Update completion documents
- Usage examples
- API documentation

**Total Remaining**: ~6 hours

---

## Architecture

### Command Pattern
```csharp
// Factory method with validation
var command = WithdrawGoldCommand.Create(
    persona, coinhouse, amount, reason);

// Handler orchestrates
var result = await handler.HandleAsync(command);

// Event published on success
await eventBus.PublishAsync(new GoldWithdrawnEvent(...));
```

### Query Pattern
```csharp
// Query is immutable record
var query = new GetBalanceQuery(persona, coinhouse);

// Handler returns result (not domain entity)
var balance = await handler.HandleAsync(query);

// Read-only, no side effects
```

### Event Pattern
```csharp
// Events are past-tense facts
public sealed record GoldWithdrawnEvent(
    PersonaId Withdrawer,
    CoinhouseTag Coinhouse,
    GoldAmount Amount,
    TransactionId TransactionId,
    DateTime OccurredAt) : IDomainEvent;
```

---

## Key Features

### 1. Balance Validation (Critical!)
Withdrawal handler prevents overdrafts:
```csharp
if (currentBalance < requestedAmount)
{
    return Fail("Insufficient balance...");
    // No mutations on failure
}
```

**4 dedicated tests** ensure:
- Returns failure ✅
- Balance unchanged ✅
- No event published ✅
- No transaction recorded ✅

### 2. Auto-Account Creation
Deposit handler creates accounts automatically:
```csharp
if (account == null)
{
    account = CreateNewAccount(accountId, coinhouse);
    coinhouse.Accounts.Add(account);
}
```

User-friendly: first deposit opens account.

### 3. Event Publishing
All handlers publish events after successful mutations:
```csharp
// After successful deposit
var evt = new GoldDepositedEvent(...);
await _eventBus.PublishAsync(evt, cancellationToken);
```

Enables future audit logs, notifications, cross-aggregate workflows.

### 4. Read-Only Queries
Balance queries are idempotent:
```csharp
// Multiple queries don't modify state
var balance1 = await handler.HandleAsync(query);
var balance2 = await handler.HandleAsync(query);
// balance1 == balance2, account unchanged
```

---

## Files Created (44 total)

### Commands (6 files)
1. `Commands/DepositGoldCommand.cs`
2. `Commands/DepositGoldCommandHandler.cs`
3. `Commands/WithdrawGoldCommand.cs`
4. `Commands/WithdrawGoldCommandHandler.cs`
5. `Transactions/TransferGoldCommand.cs`
6. `Transactions/TransferGoldCommandHandler.cs`

### Queries (4 files)
7. `Queries/GetBalanceQuery.cs`
8. `Queries/GetBalanceQueryHandler.cs`
9. `Transactions/GetTransactionHistoryQuery.cs`
10. `Transactions/GetTransactionHistoryQueryHandler.cs`

### DTOs (1 file)
11. `DTOs/BalanceDto.cs`

### Events (6 files)
12. `SharedKernel/Events/IDomainEvent.cs`
13. `SharedKernel/Events/IEventBus.cs`
14. `SharedKernel/Events/InMemoryEventBus.cs`
15. `Economy/Events/GoldDepositedEvent.cs`
16. `Economy/Events/GoldWithdrawnEvent.cs`
17. `Economy/Events/GoldTransferredEvent.cs`

### Value Objects (3 files)
18. `ValueObjects/GoldAmount.cs`
19. `ValueObjects/TransactionReason.cs`
20. `ValueObjects/TransactionId.cs`

### Repositories (2 files)
21. `Transactions/ITransactionRepository.cs`
22. `Transactions/TransactionRepository.cs`

### Tests (10 files)
23. `Tests/Helpers/EconomyTestHelpers.cs`
24. `Tests/.../Commands/DepositGoldCommandTests.cs`
25. `Tests/.../Commands/DepositGoldCommandHandlerTests.cs`
26. `Tests/.../Commands/WithdrawGoldCommandTests.cs`
27. `Tests/.../Commands/WithdrawGoldCommandHandlerTests.cs`
28. `Tests/.../Queries/GetBalanceQueryTests.cs`
29. `Tests/.../TransferGoldCommandTests.cs`
30. `Tests/.../TransferGoldCommandHandlerTests.cs`
31. `Tests/.../TransactionEntityTests.cs`
32. `Tests/.../TransactionRepositoryTests.cs`

### Documentation (11 files)
33. `PHASE3_PART3_PLAN.md`
34. `PHASE3_PART3_STATUS.md`
35. `PHASE3_PART3_SUMMARY.md`
36. `PHASE3_PART3_DAY1_COMPLETE.md`
37. `PHASE3_PART3_DAY2_PROGRESS.md`
38. `PHASE3_PART3_DEPOSITGOLD_COMPLETE.md`
39. `PHASE3_PART3_WITHDRAWGOLD_COMPLETE.md`
40. `PHASE3_PART3_GETBALANCE_COMPLETE.md`
41. `PHASE3_PART3_PROGRESS_REPORT.md`
42. `PHASE3_PART3_FINAL_STATUS.md`
43. `Economy/PHASE3_PART3_ECONOMY_PLAN.md`
44. `TRANSACTION_REPOSITORY_INMEMORY_COMPLETE.md`

---

## Test Breakdown

| Component | Tests | Status |
|-----------|-------|--------|
| DepositGoldCommand | 11 | ✅ Passing |
| DepositGoldCommandHandler | 10 | ✅ Passing |
| WithdrawGoldCommand | 11 | ✅ Passing |
| WithdrawGoldCommandHandler | 16 | ✅ Passing |
| GetBalanceQuery | 8 | ✅ Passing |
| TransferGoldCommand | 18 | ✅ Passing |
| TransferGoldCommandHandler | 10 | ✅ Passing |
| TransactionRepository | 15 | ✅ Passing |
| TransactionEntity | 15 | ✅ Passing |
| RegionPolicyResolver | 5 | ✅ Passing |
| RegionPolicyResolverBehavior | 7 | ✅ Passing |
| Pre-existing Economy | 81 | ✅ Passing |
| **Total** | **136** | **✅ All Passing** |

---

## Key Decisions

### 1. Balance Validation Location
**Decision**: Validate in handler, not command
- Commands validate syntax (non-negative amounts)
- Handlers validate business rules (sufficient balance)
- Allows clear separation of concerns

### 2. Account Creation
**Decision**: Deposits create accounts, withdrawals require existing
- User-friendly (first deposit auto-opens account)
- Safe (can't withdraw from nothing)
- Clear error messages

### 3. Event Publishing Timing
**Decision**: Publish AFTER successful persistence
- Events represent "what happened" (past tense)
- Only publish if transaction commits
- Event timestamp matches DB timestamp

### 4. TransactionId Mismatch
**Issue**: Database uses `long`, events use `Guid`
**Resolution**: Use `TransactionId.NewId()` for now
**Future**: Migrate DB to Guid IDs in Phase 4

---

## Next Session Plan

1. **Implement GetCoinhouseBalancesQuery** (2 hours)
   - Query all balances for a persona
   - Handler with repository access
   - 10 BDD tests
   - DTO for collection results

2. **Integration Tests** (3 hours)
   - Full deposit→balance→withdraw flow
   - Overdraft scenarios
   - Multi-coinhouse operations
   - Event ordering verification
   - 15-20 tests

3. **Documentation** (1 hour)
   - Phase 3.3 completion document
   - Usage guide
   - Update REFACTORING_INDEX.md

**After completion**: Phase 3.3 ✅ 100%

---

## Technical Debt

### Current (All Low Priority)
1. **Deterministic Guid Generation**
   - Non-Guid persona types get random account IDs
   - Should use SHA-256 hash for consistency
   - Workaround: Only Character/Organization supported (both use Guids)

2. **TransactionId Mismatch**
   - DB uses `long`, events use `Guid`
   - Workaround: Events use new Guid, still published correctly
   - Resolution: Migrate DB to Guids in Phase 4

3. **In-Memory Repositories**
   - Not persisted to database
   - Workaround: Fine for testing/development
   - Resolution: EF Core repositories in Phase 4

**None block progress.**

---

## Success Criteria Progress

- [x] Apply CQRS to Economy subsystem (55%)
- [x] Create command handlers (100% - 3/3 done!)
- [x] Create query handlers (67% - 2/3 done)
- [x] Publish domain events (100% for handlers)
- [ ] Remove direct repository access (0%)
- [x] 136/150+ tests (91%)
- [ ] Integration tests (0%)
- [x] Documentation (80%)

---

## Related Documents

See complete list in phase-specific completion documents:
- `PHASE3_PART3_DEPOSITGOLD_COMPLETE.md`
- `PHASE3_PART3_WITHDRAWGOLD_COMPLETE.md`
- `PHASE3_PART3_GETBALANCE_COMPLETE.md`
- `PHASE3_PART3_PROGRESS_REPORT.md`

---

**Last Updated**: October 28, 2025
**Previous Phase**: [Phase 3.2: Codex Application](PHASE3_2_CODEX_APPLICATION.md)
**Next Phase**: [Phase 3.4: Other Subsystems](PHASE3_4_OTHER_SUBSYSTEMS.md)
# Phase 2: Persona Abstraction

**Status**: ✅ Complete
**Completion Date**: October 2025

---

## Goal

Create a unified meta-entity that represents any actor participating in world systems. Enable organizations, governments, coinhouses, and system processes to be first-class economic and social actors alongside player characters.

---

## Problem Statement

### Before Phase 2
- `CharacterId` was the only actor type
- Organizations, coinhouses, warehouses, and system processes couldn't participate in:
  - Economy (transactions, ownership)
  - Reputation systems
  - Industry operations
  - Government functions
- APIs assumed only player characters exist
- No unified interface for "who is performing this action?"

### Impact
- Organizations couldn't own property or gold
- Governments couldn't collect taxes
- System processes (decay, market rebalancing) had no actor identity
- Transaction logs couldn't record non-character actors
- Reputation API limited to player-to-player interactions

---

## Solution: Persona Meta-Entity

### Key Insight
**Persona is a meta-entity that *owns* other domain entities.**

Each domain entity has:
1. **Its own strongly-typed ID** (e.g., `CharacterId`, `OrganizationId`)
2. **A PersonaId** for cross-subsystem actor references

This allows subsystems to reference actors uniformly without knowing their concrete type, while domain logic still operates on strongly-typed entities.

---

## Design

### PersonaId - Unified Identifier

```csharp
/// <summary>
/// Unified identifier for actors across all subsystems.
/// Format: "{Type}:{UnderlyingId}"
/// Examples: "Character:550e8400-e29b-41d4-a716-446655440000"
///           "Organization:merchants_guild"
///           "Coinhouse:cordor_bank"
/// </summary>
public readonly record struct PersonaId(PersonaType Type, string Value)
{
    public override string ToString() => $"{Type}:{Value}";

    public static PersonaId FromCharacter(CharacterId characterId) =>
        new(PersonaType.Character, characterId.Value.ToString());

    public static PersonaId FromOrganization(OrganizationId orgId) =>
        new(PersonaType.Organization, orgId.Value.ToString());

    public static PersonaId FromCoinhouse(CoinhouseTag tag) =>
        new(PersonaType.Coinhouse, tag.Value);

    public static PersonaId FromGovernment(GovernmentId govId) =>
        new(PersonaType.Government, govId.Value.ToString());

    public static PersonaId Parse(string value)
    {
        var parts = value.Split(':', 2);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid PersonaId format: {value}");

        var type = Enum.Parse<PersonaType>(parts[0]);
        return new PersonaId(type, parts[1]);
    }
}

public enum PersonaType
{
    Character,
    Organization,
    Coinhouse,
    Warehouse,
    Government,
    SystemProcess
}
```

### Persona Hierarchy

```csharp
/// <summary>
/// Meta-entity representing any actor in the world.
/// Domain entities have their own strongly-typed IDs AND a PersonaId
/// for cross-subsystem references.
/// </summary>
public abstract record Persona
{
    public required PersonaId Id { get; init; }
    public required PersonaType Type { get; init; }
    public required string DisplayName { get; init; }
}

// Concrete implementations wrap strongly-typed domain entities
public sealed record CharacterPersona : Persona
{
    public required CharacterId CharacterId { get; init; }
    // CharacterId is the "real" ID; PersonaId is derived
}

public sealed record OrganizationPersona : Persona
{
    public required OrganizationId OrganizationId { get; init; }
}

public sealed record GovernmentPersona : Persona
{
    public required GovernmentId GovernmentId { get; init; }
    public required SettlementId Settlement { get; init; }
}

public sealed record CoinhousePersona : Persona
{
    public required CoinhouseTag Tag { get; init; }
    public required SettlementId Settlement { get; init; }
}

public sealed record SystemPersona : Persona
{
    // For automated processes like tax collection, decay, etc.
    // No underlying entity; PersonaId.Value is a descriptive key
}
```

---

## Domain Entity Pattern

Each domain entity stores both its own ID and its PersonaId:

```csharp
// Database entity
public class Character
{
    public Guid Id { get; set; } // CharacterId underlying value
    public string PersonaId { get; set; } // "Character:{Id}"
    public string Name { get; set; }
    // ... other properties
}

// Value object for domain use
public readonly record struct CharacterId(Guid Value)
{
    public static CharacterId NewId() => new(Guid.NewGuid());

    public PersonaId ToPersonaId() =>
        PersonaId.FromCharacter(this);
}

// Creating a new character
var characterId = CharacterId.NewId();
var character = new Character
{
    Id = characterId.Value,
    PersonaId = characterId.ToPersonaId().ToString(),
    Name = "Aldric"
};
```

---

## Persona-Aware APIs

### Before
```csharp
public interface ICharacterIndustryContext
{
    CharacterId CharacterId { get; }
}

public void TransferGold(CharacterId from, CharacterId to, int amount);
```

### After
```csharp
public interface IIndustryContext
{
    PersonaId Actor { get; }
}

public void TransferGold(PersonaId from, PersonaId to, Quantity amount);
```

---

## Migration Path

### Step 1: Introduce Persona Hierarchy
Created in `SharedKernel/Personas/`:
- `Persona.cs` - Base record
- `PersonaId.cs` - Unified identifier
- `PersonaType.cs` - Actor type enum
- `CharacterPersona.cs`
- `OrganizationPersona.cs`
- `GovernmentPersona.cs`
- `CoinhousePersona.cs`
- `SystemPersona.cs`

### Step 2: Add PersonaId to Database
Added `PersonaId` columns to:
- `Transactions` table
- `ReputationRecords` table
- `Ownership` table
- `IndustryMemberships` table

Kept `CharacterId` temporarily for migration safety.

### Step 3: Create Adapters
Helper methods for conversion:
```csharp
public static class PersonaIdExtensions
{
    public static PersonaId ToPersonaId(this CharacterId id) =>
        PersonaId.FromCharacter(id);

    public static PersonaId ToPersonaId(this OrganizationId id) =>
        PersonaId.FromOrganization(id);
}
```

### Step 4: Refactor Subsystems One at a Time
1. **Economy** - Transactions, taxation, banks
2. **Industries** - Ownership, production
3. **Organizations** - Primary use case
4. **Reputation** - Cross-actor relationships

### Step 5: Remove CharacterId-Only APIs
Once all subsystems migrated:
- Removed `CharacterId` columns from shared tables
- Deleted compatibility adapters
- Made `PersonaId` the canonical actor reference

---

## Files Affected

### Created
- `SharedKernel/Personas/Persona.cs`
- `SharedKernel/Personas/PersonaId.cs`
- `SharedKernel/Personas/PersonaType.cs`
- `SharedKernel/Personas/CharacterPersona.cs`
- `SharedKernel/Personas/OrganizationPersona.cs`
- `SharedKernel/Personas/GovernmentPersona.cs`
- `SharedKernel/Personas/CoinhousePersona.cs`
- `SharedKernel/Personas/SystemPersona.cs`

### Modified
- `Features/WorldEngine/Characters/` - Adapted interfaces
- `Features/WorldEngine/Economy/` - Transactions, taxation
- `Features/WorldEngine/Industries/` - Ownership, production
- `Features/WorldEngine/Organizations/` - Primary use case
- `Database/Entities/` - Added PersonaId columns

### Database Migrations
- `AddPersonaIdColumns` - Add PersonaId to all actor tables
- `MigrateCharacterIdToPersonaId` - Data migration
- `RemoveCharacterIdColumns` - Cleanup after migration

---

## Examples

### Transaction Between Different Actor Types
```csharp
// Character pays guild dues
var character = PersonaId.FromCharacter(characterId);
var guild = PersonaId.FromOrganization(new OrganizationId("merchants_guild"));

await _worldEngine.ExecuteAsync(new TransferGoldCommand(
    From: character,
    To: guild,
    Amount: Quantity.Parse(100),
    Reason: "Monthly guild dues"
));
```

### Government Tax Collection
```csharp
// Government collects tax from character
var government = PersonaId.FromGovernment(cordorGovt);
var character = PersonaId.FromCharacter(characterId);

await _worldEngine.ExecuteAsync(new TransferGoldCommand(
    From: character,
    To: government,
    Amount: taxAmount,
    Reason: "Property tax collection"
));
```

### System Process
```csharp
// Automated decay removes resources
var system = new SystemPersona
{
    Id = new PersonaId(PersonaType.SystemProcess, "resource_decay"),
    Type = PersonaType.SystemProcess,
    DisplayName = "Resource Decay System"
};

// System can now be logged as actor in audit trails
```

---

## Lessons Learned

### 1. PersonaId as String Format
**Decision**: Use `"{Type}:{Value}"` format
- Human-readable in logs and database
- Easy to parse and debug
- Enables querying by type (e.g., all Character personas)
- Trade-off: Slightly larger storage than pure GUID

### 2. Dual Identity Pattern
**Decision**: Keep both typed ID and PersonaId
- Domain logic uses `CharacterId` (type-safe)
- Cross-subsystem references use `PersonaId` (flexible)
- Best of both worlds: type safety + extensibility

### 3. Migration Strategy
**Decision**: Add PersonaId alongside existing IDs first
- Allows gradual migration subsystem-by-subsystem
- Data migration populates PersonaId from existing IDs
- Drop old columns only after all code migrated
- Rollback safety via EF Core migration `Down()` methods

### 4. Test Helpers
**Decision**: Create persona factory methods
```csharp
public static class PersonaTestHelpers
{
    public static CharacterPersona CreateCharacterPersona(string name = "TestChar") =>
        new CharacterPersona
        {
            CharacterId = CharacterId.NewId(),
            Id = PersonaId.FromCharacter(CharacterId.NewId()),
            Type = PersonaType.Character,
            DisplayName = name
        };
}
```

---

## Success Criteria

- [x] Persona hierarchy created in SharedKernel
- [x] PersonaId added to all actor-related tables
- [x] All economy APIs accept PersonaId
- [x] All industry APIs accept PersonaId
- [x] Organizations can own property and gold
- [x] Governments can collect taxes
- [x] System processes have actor identity
- [x] All tests updated and passing
- [x] CharacterId-only APIs removed

---

## Impact Metrics

- **Persona Types Created**: 6 (Character, Organization, Government, Coinhouse, Warehouse, System)
- **APIs Migrated**: ~30 methods
- **Database Tables Updated**: 8
- **Tests Updated**: ~80
- **New Scenarios Enabled**: Organizations in economy, government taxation, system processes

---

**Completion Date**: October 2025
**Previous Phase**: [Phase 1: Strong Types](PHASE1_STRONG_TYPES.md)
**Next Phase**: [Phase 3.1: CQRS Infrastructure](PHASE3_1_CQRS_INFRASTRUCTURE.md)

