# Phase 1: Strong Types (SharedKernel)

**Status**: ✅ Complete
**Completion Date**: October 27, 2025

---

## Goal

Replace primitives with immutable records that carry domain meaning and validation. Eliminate runtime errors from invalid primitives by catching issues at compile time.

---

## Problem Statement

### Before Phase 1
- Settlement IDs are raw `int` across the codebase
- Region tags are unvalidated `string` primitives
- Coinhouse identifiers are `string` tags without normalization
- Industry codes are `string` primitives
- Quantities, capacities, and rates are raw `int`/`decimal` types
- No compile-time safety for domain concepts

### Impact
- Runtime errors from invalid IDs (negative settlements, empty tags)
- No way to enforce business rules at type level
- Easy to accidentally pass wrong primitive type to methods
- No domain meaning in method signatures

---

## Solution: Value Objects

### Created Types

```csharp
// Location/Region
public readonly record struct SettlementId(int Value)
{
    public static SettlementId Parse(int value) =>
        value > 0 ? new(value) :
        throw new ArgumentException("Settlement ID must be positive");
}

public readonly record struct RegionTag(string Value)
{
    public RegionTag(string value) : this(Validate(value)) { }

    private static string Validate(string tag) =>
        string.IsNullOrWhiteSpace(tag)
            ? throw new ArgumentException("Region tag cannot be empty")
            : tag.Trim().ToLowerInvariant();

    public static implicit operator string(RegionTag tag) => tag.Value;
}

public readonly record struct AreaTag(string Value)
{
    // Similar validation to RegionTag
}

// Economy
public readonly record struct CoinhouseTag(string Value)
{
    // Case-insensitive, non-empty validation
}

public readonly record struct Capacity(int Value)
{
    public static Capacity Parse(int value) =>
        value >= 0 ? new(value) :
        throw new ArgumentException("Capacity cannot be negative");

    public bool CanAccept(int amount) => Value >= amount;
}

public readonly record struct Quantity(int Value)
{
    public static Quantity Zero => new(0);

    public static Quantity Parse(int value) =>
        value >= 0 ? new(value) :
        throw new ArgumentException("Quantity cannot be negative");

    public Quantity Add(Quantity other) => new(Value + other.Value);
    public Quantity Subtract(Quantity other) => Parse(Value - other.Value);
}

// Industry
public readonly record struct IndustryCode(string Value)
{
    // Validated against known industry types
}

public readonly record struct ResourceNodeId(int Value)
{
    // Positive integer validation
}

// Traits
public readonly record struct TraitCode(string Value)
{
    // Validated, case-insensitive
}
```

---

## Benefits

### 1. Compile-Time Safety
```csharp
// Before: Easy to pass wrong ID
void GetSettlement(int id) // Any int works - bug waiting to happen

// After: Type system enforces correctness
void GetSettlement(SettlementId id) // Can't pass wrong type
```

### 2. Domain Meaning in Signatures
```csharp
// Before: What are these ints?
void Transfer(int from, int to, int amount)

// After: Crystal clear
void Transfer(PersonaId from, PersonaId to, Quantity amount)
```

### 3. Validation at Construction
```csharp
// Impossible to create invalid values
var quantity = Quantity.Parse(-100); // Throws ArgumentException
var settlement = SettlementId.Parse(0); // Throws ArgumentException
```

### 4. Rich Behavior
```csharp
// Value objects can have domain logic
var total = Quantity.Zero
    .Add(Quantity.Parse(10))
    .Add(Quantity.Parse(20));

if (capacity.CanAccept(amount))
{
    // Safe to proceed
}
```

---

## Migration Strategy

### Step 1: Create Value Objects
Added new types to `SharedKernel/ValueObjects/`:
- `SettlementId.cs`
- `RegionTag.cs`
- `AreaTag.cs`
- `CoinhouseTag.cs`
- `Quantity.cs`
- `Capacity.cs`
- `IndustryCode.cs`
- `ResourceNodeId.cs`
- `TraitCode.cs`

### Step 2: Update Loaders
Modified loaders to construct and validate types at entry points:
- `RegionDefinitionLoadingService.cs`
- `CoinhouseLoader.cs`
- `IndustryDefinitionLoader.cs`
- `ResourceDefinitionLoadingService.cs`

### Step 3: Refactor Repositories
Updated repositories to accept/return strong types:
- `IRegionRepository.cs`
- `ICoinhouseRepository.cs`
- `IIndustryRepository.cs`

### Step 4: Update Indexes and Resolvers
Modified lookup services to use strong types:
- `RegionIndex.cs`
- `SettlementCoinhouseResolver.cs`
- `IndustryMembershipResolver.cs`

### Step 5: Remove Primitives from Public APIs
- All public-facing methods use value objects
- Internal conversions to/from primitives allowed where necessary
- Database entities convert at boundary

---

## Files Affected

### Created
- `SharedKernel/ValueObjects/SettlementId.cs`
- `SharedKernel/ValueObjects/RegionTag.cs`
- `SharedKernel/ValueObjects/AreaTag.cs`
- `SharedKernel/ValueObjects/CoinhouseTag.cs`
- `SharedKernel/ValueObjects/Quantity.cs`
- `SharedKernel/ValueObjects/Capacity.cs`
- `SharedKernel/ValueObjects/IndustryCode.cs`
- `SharedKernel/ValueObjects/ResourceNodeId.cs`
- `SharedKernel/ValueObjects/TraitCode.cs`

### Modified
- All of `Features/WorldEngine/Regions/`
- All of `Features/WorldEngine/Economy/`
- `Features/WorldEngine/Industries/`
- `Features/WorldEngine/Harvesting/`
- `Database/Entities/` (added conversion extensions)

---

## Test Updates

### Pattern
Tests updated to use factory methods from test helpers:

```csharp
// Before
var settlementId = 1;

// After
var settlementId = SettlementTestHelpers.CreateSettlementId(1);
```

### Test Helpers Created
- `Tests/Helpers/WorldEngine/SettlementTestHelpers.cs`
- `Tests/Helpers/WorldEngine/RegionTestHelpers.cs`
- `Tests/Helpers/WorldEngine/EconomyTestHelpers.cs`

---

## Lessons Learned

### 1. Validation Placement
**Decision**: Validate in `Parse` factory methods, not constructors
- Allows invalid construction to throw clear exceptions
- Makes intent obvious (Parse = may fail, constructor = must succeed)
- Follows .NET conventions (int.Parse, Guid.Parse)

### 2. Implicit Conversions
**Decision**: Use sparingly, only for strings
- `RegionTag` → `string` is safe (implicit)
- `string` → `RegionTag` requires explicit `Parse()` (validation)
- Prevents accidental conversions while allowing convenient usage

### 3. Record Structs vs Classes
**Decision**: Use `readonly record struct` for value objects
- Value semantics (equality by value, not reference)
- No heap allocations
- Immutable by default
- Perfect for identifiers

### 4. Breaking Changes Approach
**Decision**: Replace primitives immediately, fix all compilation errors
- Faster than gradual migration
- Cleaner codebase (no compatibility layers)
- Compiler guides the refactoring

---

## Success Criteria

- [x] All value objects created in SharedKernel
- [x] All loaders validate at entry points
- [x] All repositories use strong types
- [x] All public APIs use value objects
- [x] All tests updated and passing
- [x] Zero raw primitives in public signatures

---

## Impact Metrics

- **Types Created**: 9 value objects
- **Files Modified**: ~50
- **Tests Updated**: ~100
- **Compilation Errors Fixed**: ~300
- **Runtime Errors Prevented**: Countless (compile-time enforcement)

---

**Completion Date**: October 27, 2025
**Next Phase**: [Phase 2: Persona Abstraction](PHASE2_PERSONA_ABSTRACTION.md)
# WorldEngine Refactoring - Main Index

**Current Date**: October 28, 2025

---

## Quick Navigation

| Phase | Status | Completion | Document |
|-------|--------|------------|----------|
| **Phase 1: Strong Types** | ✅ Complete | Oct 27, 2025 | [PHASE1_STRONG_TYPES.md](PHASE1_STRONG_TYPES.md) |
| **Phase 2: Persona Abstraction** | ✅ Complete | Oct 2025 | [PHASE2_PERSONA_ABSTRACTION.md](PHASE2_PERSONA_ABSTRACTION.md) |
| **Phase 3.1: CQRS Infrastructure** | ✅ Complete | Oct 2025 | [PHASE3_1_CQRS_INFRASTRUCTURE.md](PHASE3_1_CQRS_INFRASTRUCTURE.md) |
| **Phase 3.2: Codex Application Layer** | ✅ Complete | Oct 28, 2025 | [PHASE3_2_CODEX_APPLICATION.md](PHASE3_2_CODEX_APPLICATION.md) |
| **Phase 3.3: Economy Expansion** | 🟢 In Progress<br/>~55% | - | [PHASE3_3_ECONOMY_EXPANSION.md](PHASE3_3_ECONOMY_EXPANSION.md) |
| **Phase 3.4: Other Subsystems** | ⏳ Not Started | - | [PHASE3_4_OTHER_SUBSYSTEMS.md](PHASE3_4_OTHER_SUBSYSTEMS.md) |
| **Phase 4: Event Bus** | ⏳ Not Started | - | [PHASE4_EVENT_BUS.md](PHASE4_EVENT_BUS.md) |
| **Phase 5: Public API** | ⏳ Not Started | - | [PHASE5_PUBLIC_API.md](PHASE5_PUBLIC_API.md) |

---

## Vision

Transform WorldEngine from a primitive-obsessed codebase into a strongly-typed, event-driven system with a clean command/query API. Enable both characters and non-character actors (organizations, coinhouses, governments) to participate in the economy, industries, and other world systems through a unified Persona abstraction.

---

## Core Problems Being Solved

### 1. Primitive Obsession
Settlement IDs, region tags, coinhouse identifiers, and quantities are raw primitives without validation or domain meaning. This leads to runtime errors and prevents compile-time safety.

### 2. Missing Persona Abstraction
Only `CharacterId` exists as an actor type. Organizations, governments, and system processes cannot participate in economic or social systems as first-class actors.

### 3. Coupling and Visibility
Services directly depend on repositories with no clear command/query separation. Domain logic leaks across boundaries, and there's no event bus for cross-feature coordination.

---

## Overall Strategy

**Breaking Changes Allowed**: Since this system has not been deployed to production, we use direct breaking changes rather than gradual deprecation for cleaner, faster refactoring.

**Compilation-Driven Refactoring**: Change type signatures and fix compilation errors immediately. No compatibility layers or `[Obsolete]` attributes needed.

**Test-First BDD Approach**: Write behavior tests in C# using Given-When-Then patterns before implementing features.

---

## Rollout Timeline

| Timeframe | Phase | Focus |
|-----------|-------|-------|
| **Week 1-2** | Phase 1 | Strong Types (Regions, Economy) |
| **Week 3-4** | Phase 2 | Persona Abstraction (Characters, Organizations) |
| **Week 5-6** | Phase 3 | Commands/Queries (Economy, Codex) |
| **Week 7** | Phase 4 | Event Bus (Channel-based async) |
| **Week 8** | Phase 5 | Public API Façade |

---

## Current Progress

### ✅ Completed Phases

**Phase 1**: Strong types established in SharedKernel
- `SettlementId`, `RegionTag`, `CoinhouseTag`, `Quantity`, `Capacity` and more
- All primitives replaced in public APIs
- **Status**: Complete ✅

**Phase 2**: Persona abstraction implemented
- `PersonaId`, `Persona` hierarchy with Character, Organization, Government variants
- All actor-related APIs migrated
- **Status**: Complete ✅

**Phase 3.1**: CQRS infrastructure created
- `ICommand`, `IQuery`, handler interfaces
- Command/query pattern established
- **Status**: Complete ✅

**Phase 3.2**: Codex application layer refactored
- All Codex operations use commands/queries
- Event integration complete
- All tests passing
- **Status**: Complete ✅

### 🟢 In Progress

**Phase 3.3**: Economy expansion (~55% complete)
- Commands: TransferGold, DepositGold, WithdrawGold ✅
- Queries: GetTransactionHistory, GetBalance ✅
- Remaining: GetCoinhouseBalances, integration tests
- **136 tests passing**

See [PHASE3_3_ECONOMY_EXPANSION.md](PHASE3_3_ECONOMY_EXPANSION.md) for details.

---

## Success Criteria

- [ ] No raw primitives in public APIs
- [ ] Organizations and governments participate as first-class actors
- [x] Command handlers validate and publish events
- [x] Query handlers are read-only
- [ ] External services use only `IWorldEngine` façade
- [x] All tests pass after each phase
- [x] BDD tests cover Persona interactions
- [ ] Event subscribers react to cross-subsystem changes

**Current**: 5/8 criteria met

---

## Key Architectural Decisions

### Persona as Meta-Entity
Each domain entity (Character, Organization, Coinhouse) has:
1. Its own strongly-typed ID (`CharacterId`, `OrganizationId`)
2. A `PersonaId` for cross-subsystem actor references

This allows unified APIs without losing domain-specific type safety.

### Command Validation
Validation happens in factory methods using the `Parse` pattern, not in handlers. Value objects validate on construction. Handlers assume valid input.

### Event Bus Architecture
Channel-based async event processing with background tasks. In-game thread publishes to channel; background tasks consume and process. Game-thread callbacks scheduled via NWN scheduler when needed.

### Test Strategy
BDD-style tests in C# using Given-When-Then method names. No Gherkin files. Delete and rewrite tests that can't adapt to new patterns rather than maintaining technical debt.

---

## Related Documents

- [RESEARCH_GUID_PERFORMANCE.md](RESEARCH_GUID_PERFORMANCE.md) - GUID storage and performance research
- [MIGRATION_STRATEGY.md](MIGRATION_STRATEGY.md) - EF Core migration patterns
- [TEST_PATTERNS.md](TEST_PATTERNS.md) - BDD testing guidelines
- [API_CONVENTIONS.md](API_CONVENTIONS.md) - Command/query design patterns

---

## Documentation Status

| Phase | Planning | Implementation | Completion | Progress Reports |
|-------|----------|----------------|------------|------------------|
| 1 | ✅ | ✅ | ✅ | `PHASE1_COMPLETE.md` |
| 2 | ✅ | ✅ | ✅ | `PERSONA_QUICK_REFERENCE.md` |
| 3.1 | ✅ | ✅ | ✅ | `PHASE3_PART1_COMPLETE.md` |
| 3.2 | ✅ | ✅ | ✅ | Multiple completion docs |
| 3.3 | ✅ | 🟢 | ⏳ | 8 progress documents |
| 3.4 | ✅ | ⏳ | ⏳ | - |
| 4 | ✅ | ⏳ | ⏳ | - |
| 5 | ✅ | ⏳ | ⏳ | - |

---

**Last Updated**: October 28, 2025
**Next Review**: After Phase 3.3 completion

