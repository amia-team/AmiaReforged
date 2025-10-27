# WorldEngine Refactoring Plan

## Vision
Transform WorldEngine from a primitive-obsessed codebase into a strongly-typed, event-driven system with a clean command/query API. Enable both characters and non-character actors (organizations, systems, NPCs) to participate in the economy, industries, and other world systems through a unified Persona abstraction.

## Core Problems

### 1. Primitive Obsession
- Settlement IDs are `int` across the codebase
- Region tags are `string` without validation
- Coinhouse identifiers are `string` tags
- Industry codes are `string` primitives
- Quantities, capacities, and rates are raw `int`/`decimal` types
- No compile-time safety for domain concepts

### 2. Missing Persona Abstraction
- `CharacterId` is the only actor type
- Organizations, coinhouses, warehouses, and system processes cannot be first-class economic actors
- No unified interface for "who is performing this action?"
- Reputation, ownership, and transaction APIs assume only player characters exist

**Key Insight:** Persona is a meta-entity that *owns* other domain entities. Each domain entity (Character, Organization, Coinhouse, etc.) has:
1. Its own strongly-typed ID (e.g., `CharacterId`, `OrganizationId`)
2. A `PersonaId` for cross-subsystem actor references (transactions, reputation, ownership)

This allows subsystems to reference actors uniformly without knowing their concrete type, while domain logic still operates on strongly-typed entities.

### 3. Coupling and Visibility
- Services directly depend on repositories
- No clear command/query separation
- Domain logic leaks into loaders and repositories
- Missing event bus for cross-feature coordination

## Phase 1: Introduce Strong Types (SharedKernel)

### Goal
Replace primitives with immutable records that carry domain meaning and validation.

### New Value Objects

```csharp
// Location/Region
public readonly record struct SettlementId(int Value)
{
    public static SettlementId Parse(int value) =>
        value > 0 ? new(value) : throw new ArgumentException("Settlement ID must be positive", nameof(value));
}

public readonly record struct RegionTag(string Value)
{
    public RegionTag(string value) : this(Validate(value)) { }

    private static string Validate(string tag) =>
        string.IsNullOrWhiteSpace(tag)
            ? throw new ArgumentException("Region tag cannot be empty", nameof(tag))
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
    // Case-insensitive, non-empty
}

public readonly record struct Capacity(int Value)
{
    public static Capacity Parse(int value) =>
        value >= 0 ? new(value) : throw new ArgumentException("Capacity cannot be negative");

    public bool CanAccept(int amount) => Value >= amount;
}

public readonly record struct Quantity(int Value)
{
    public static Quantity Zero => new(0);
    public static Quantity Parse(int value) =>
        value >= 0 ? new(value) : throw new ArgumentException("Quantity cannot be negative");

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
    // Positive integer
}

// Traits
public readonly record struct TraitCode(string Value)
{
    // Validated, case-insensitive
}
```

### Migration Strategy
1. Add new types to `SharedKernel/ValueObjects/`
2. Update loaders to construct and validate these types at entry points
3. Refactor repositories to accept/return strong types
4. Update indexes and resolvers
5. Remove raw primitives from public APIs (internal conversions OK)

**Files Affected:**
- All of `Features/WorldEngine/Regions/`
- All of `Features/WorldEngine/Economy/`
- `Features/WorldEngine/Industries/`
- `Features/WorldEngine/Harvesting/`
- `Database/Entities/` (add conversion extensions)

---

## Phase 2: Persona Abstraction

### Goal
Create a unified meta-entity that represents any actor participating in world systems. Domain entities (Character, Organization, Coinhouse, etc.) maintain their own strongly-typed IDs while also having a PersonaId for cross-subsystem references.

### Design

```csharp
namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Meta-entity representing any actor in the world.
/// Domain entities have their own strongly-typed IDs AND a PersonaId for cross-subsystem references.
/// </summary>
public abstract record Persona
{
    public required PersonaId Id { get; init; }
    public required PersonaType Type { get; init; }
    public required string DisplayName { get; init; }
}

/// <summary>
/// Unified identifier for actors across all subsystems.
/// Format: "{Type}:{UnderlyingId}" (e.g., "Character:550e8400-e29b-41d4-a716-446655440000")
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

// Concrete implementations - each wraps a strongly-typed domain entity
public sealed record CharacterPersona : Persona
{
    public required CharacterId CharacterId { get; init; }
    // CharacterId is the "real" ID; PersonaId is derived for cross-subsystem use
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
    // For automated processes like tax collection, decay, market rebalancing
    // No underlying entity; PersonaId.Value is a descriptive key
}
```

### Domain Entity Pattern

Each domain entity stores both its own ID and its PersonaId:

```csharp
// Database entity
public class Character
{
    public Guid Id { get; set; } // CharacterId underlying value
    public string PersonaId { get; set; } // "Character:{Id}" for cross-subsystem references
    public string Name { get; set; }
    // ... other properties
}

// Value object for domain use
public readonly record struct CharacterId(Guid Value)
{
    public static CharacterId NewId() => new(Guid.NewGuid());
    public PersonaId ToPersonaId() => PersonaId.FromCharacter(this);
}

// When creating a new character:
var characterId = CharacterId.NewId();
var character = new Character
{
    Id = characterId.Value,
    PersonaId = characterId.ToPersonaId().ToString(), // Stored for queries
    Name = "Aldric"
};
```

### Persona-Aware Services

Update key services to accept `Persona` instead of `CharacterId`:

```csharp
// Before
public interface ICharacterIndustryContext
{
    CharacterId CharacterId { get; }
}

// After
public interface IIndustryContext
{
    PersonaId Actor { get; }
}

// Before
public void TransferGold(CharacterId from, CharacterId to, int amount);

// After
public void TransferGold(PersonaId from, PersonaId to, Quantity amount);
```

### Migration Path
1. Introduce `Persona` hierarchy in `SharedKernel/Personas/`
2. Add `PersonaId` to reputation, ownership, and transaction tables (keep `CharacterId` for now)
3. Create adapters: `CharacterId` → `PersonaId` helpers
4. Refactor one subsystem at a time (Economy → Industries → Organizations)
5. Deprecate `CharacterId`-only APIs once all subsystems support `Persona`

**Files Affected:**
- `Features/WorldEngine/Characters/` (adapt existing interfaces)
- `Features/WorldEngine/Economy/` (transactions, taxation, banks)
- `Features/WorldEngine/Industries/` (ownership, production)
- `Features/WorldEngine/Organizations/` (primary use case)
- `Database/Entities/` (add `PersonaId` columns)

---

## Phase 3: Command/Query Separation (CQRS-lite)

### Goal
Establish a clear API boundary between command execution and data queries. Prepare for event sourcing.

### Structure

```
Features/WorldEngine/
  Commands/
    Economy/
      TransferGoldCommand.cs
      DepositToCoinhouseCommand.cs
    Industries/
      StartProductionCommand.cs
      HarvestNodeCommand.cs
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

  Events/ (new)
    Economy/
      GoldTransferredEvent.cs
      CoinhouseDepositMadeEvent.cs
    Industries/
      ProductionStartedEvent.cs
      ResourceHarvestedEvent.cs
```

### Command Handler Pattern

```csharp
public interface ICommand { }
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken ct);
}

public readonly record struct CommandResult(bool Success, string? ErrorMessage = null);

// Example
public sealed record TransferGoldCommand(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    string Reason) : ICommand;

[ServiceBinding(typeof(ICommandHandler<TransferGoldCommand>))]
public sealed class TransferGoldCommandHandler : ICommandHandler<TransferGoldCommand>
{
    private readonly IPersonaRepository _personas;
    private readonly IEventBus _eventBus;

    public async Task<CommandResult> HandleAsync(TransferGoldCommand cmd, CancellationToken ct)
    {
        // Validate, execute, publish event
        var evt = new GoldTransferredEvent(cmd.From, cmd.To, cmd.Amount, DateTime.UtcNow);
        await _eventBus.PublishAsync(evt, ct);
        return new CommandResult(true);
    }
}
```

### Query Handler Pattern

```csharp
public interface IQuery<TResult> { }
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

// Example
public sealed record GetCoinhouseBalanceQuery(CoinhouseTag Tag) : IQuery<Quantity>;

[ServiceBinding(typeof(IQueryHandler<GetCoinhouseBalanceQuery, Quantity>))]
public sealed class GetCoinhouseBalanceQueryHandler : IQueryHandler<GetCoinhouseBalanceQuery, Quantity>
{
    private readonly ICoinhouseRepository _coinhouses;

    public async Task<Quantity> HandleAsync(GetCoinhouseBalanceQuery query, CancellationToken ct)
    {
        var coinhouse = await _coinhouses.GetByTagAsync(query.Tag, ct);
        return coinhouse?.Balance ?? Quantity.Zero;
    }
}
```

### Migration Path
1. Create `Commands/`, `Queries/`, `Events/` directories
2. Implement `ICommandHandler` and `IQueryHandler` base infrastructure
3. Refactor one service method at a time into command/query pairs
4. Keep old service methods as thin wrappers calling handlers
5. Introduce simple in-memory `IEventBus` (phase 4 for full event sourcing)

**Files Affected:**
- All services in `Features/WorldEngine/Economy/`
- All services in `Features/WorldEngine/Industries/`
- `CoinhouseService.cs`, `OrganizationService.cs`, etc.

---

## Phase 4: Event Bus and Domain Events

### Goal
Enable loose coupling between subsystems. Allow features to react to world changes without direct dependencies.

### Event Infrastructure

```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    PersonaId Actor { get; }
}

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IDomainEvent;
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent;
}

// In-memory implementation (migrate to persistent store later)
[ServiceBinding(typeof(IEventBus))]
public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IDomainEvent
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var handlers)) return;

        foreach (var handler in handlers.Cast<Func<TEvent, CancellationToken, Task>>())
        {
            await handler(@event, ct);
        }
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
    {
        if (!_handlers.ContainsKey(typeof(TEvent)))
            _handlers[typeof(TEvent)] = new List<Delegate>();

        _handlers[typeof(TEvent)].Add(handler);
    }
}
```

### Example Events

```csharp
public sealed record GoldTransferredEvent(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public PersonaId Actor => From;
}

public sealed record ResourceHarvestedEvent(
    PersonaId Harvester,
    ResourceNodeId NodeId,
    IndustryCode ResourceType,
    Quantity Amount,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public PersonaId Actor => Harvester;
}

public sealed record ProductionCompletedEvent(
    PersonaId Producer,
    IndustryCode IndustryType,
    Quantity OutputProduced,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public PersonaId Actor => Producer;
}
```

### Migration Path
1. Implement `IEventBus` and register in DI
2. Update command handlers to publish events after mutations
3. Create event subscribers for cross-cutting concerns (logging, analytics, achievements)
4. Wire up inter-subsystem reactions (e.g., tax collection on gold transfers)
5. Add persistent event store for audit trail (optional, later phase)

---

## Phase 5: Public API Layer

### Goal
Provide a simple, discoverable API for other developers to interact with WorldEngine without coupling to internal structure.

### Façade Design

```csharp
namespace AmiaReforged.PwEngine.Features.WorldEngine;

/// <summary>
/// Primary entry point for WorldEngine interactions.
/// Abstracts commands, queries, and events behind a unified interface.
/// </summary>
[ServiceBinding(typeof(IWorldEngine))]
public interface IWorldEngine
{
    // Commands
    Task<CommandResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken ct = default)
        where TCommand : ICommand;

    // Queries
    Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken ct = default)
        where TQuery : IQuery<TResult>;

    // Events
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;
}

public sealed class WorldEngine : IWorldEngine
{
    private readonly IServiceProvider _services;
    private readonly IEventBus _eventBus;

    public async Task<CommandResult> ExecuteAsync<TCommand>(TCommand command, CancellationToken ct)
        where TCommand : ICommand
    {
        var handler = _services.GetRequiredService<ICommandHandler<TCommand>>();
        return await handler.HandleAsync(command, ct);
    }

    public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
        where TQuery : IQuery<TResult>
    {
        var handler = _services.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent
    {
        _eventBus.Subscribe(handler);
    }
}
```

### Example Usage

```csharp
// From a NWN script service
public class PlayerGoldTransferService
{
    private readonly IWorldEngine _world;

    public async Task TransferGold(NwPlayer from, NwPlayer to, int amount)
    {
        var cmd = new TransferGoldCommand(
            From: PersonaId.FromCharacter(new CharacterId(from.ControlledCreature.UUID)),
            To: PersonaId.FromCharacter(new CharacterId(to.ControlledCreature.UUID)),
            Amount: Quantity.Parse(amount),
            Reason: "Player-initiated transfer"
        );

        var result = await _world.ExecuteAsync(cmd);
        if (!result.Success)
        {
            from.SendServerMessage($"Transfer failed: {result.ErrorMessage}");
        }
    }
}
```

---

## Dependency and Testing Impact

### Breaking Changes Strategy
**Note:** Since this system has not been deployed to production, we will use direct breaking changes rather than gradual deprecation. This allows for cleaner, faster refactoring without maintaining parallel APIs.

#### Changes That Will Break Existing Code
- Any code directly instantiating `CharacterId` for non-character actors → **must migrate** to `Persona`
- Any service method accepting raw `int` settlement IDs → **must update** to `SettlementId`
- Any repository expecting `string` region tags → **must update** to `RegionTag`
- Any direct repository dependencies → **must refactor** to use command/query handlers

#### Migration Approach (Breaking Changes Allowed)
- **Phase 1:** Introduce strong types and **immediately replace** primitives in all public APIs
  - Update all loaders, repositories, and services in a single pass
  - Fix compilation errors across the codebase as they arise
  - No `[Obsolete]` attributes needed

- **Phase 2:** Introduce `Persona` and **directly replace** `CharacterId` in all actor contexts
  - Update all transaction, ownership, and reputation APIs to accept `PersonaId`
  - Refactor existing services to use the new abstraction
  - Database migrations add `PersonaId` columns; drop old columns once migrated

- **Phase 3:** Introduce CQRS and **refactor services** to use handlers
  - Replace existing service methods with command/query handlers
  - Remove old service methods entirely once handlers are implemented
  - No wrapper compatibility layer needed

- **Phase 4:** Introduce event bus and **require** event publishing in all mutations
  - Command handlers must publish events; no silent mutations allowed
  - Refactor existing side effects into event subscribers

- **Phase 5:** Lock down public API to **only** `IWorldEngine`
  - Mark internal repositories and services as `internal` where possible
  - External code must use the façade; direct access is compilation error

### Test Updates
- **Immediately update** all test fixtures to use strong types as they're introduced
- Add test helpers for creating test `Persona` instances in `Tests/Helpers/PersonaTestHelpers.cs`
- Mock `IEventBus` in unit tests to verify events are published (no silent mutations)
- Integration tests use real `InMemoryEventBus` to verify cross-subsystem reactions
- **Delete and rewrite** tests that cannot be easily adapted to new abstractions

### Compilation-Driven Refactoring
Since breaking changes are acceptable, we can leverage the compiler to guide migration:

1. **Phase 1:** Change type signatures → fix all compilation errors
2. **Phase 2:** Introduce `Persona` → fix all type mismatches
3. **Phase 3:** Remove old service methods → fix all call sites
4. **Phase 4:** Require event publishing → add event assertions to tests
5. **Phase 5:** Restrict access to internals → fix any remaining direct dependencies

This approach is faster and results in cleaner code without technical debt from compatibility layers.

---

## Rollout Order

**Aggressive Timeline:** Since we can break existing code, we move faster by fixing compilation errors immediately rather than maintaining compatibility layers.

1. **Week 1-2:** Phase 1 (Strong Types in SharedKernel)
   - Focus: Regions, Economy core types
   - Deliverable: `SettlementId`, `RegionTag`, `CoinhouseTag`, `Quantity`, `Capacity`
   - **Breaking:** Replace all primitive `int`/`string` in public APIs immediately
   - Fix all compilation errors in loaders, repositories, services, and tests

2. **Week 3-4:** Phase 2 (Persona Abstraction)
   - Focus: Characters, Organizations, Coinhouses
   - Deliverable: `Persona` hierarchy, `PersonaId`, migration complete
   - **Breaking:** Replace `CharacterId` with `PersonaId` in all actor-related APIs
   - Add `PersonaId` columns to database; migrate existing data; drop old columns
   - Fix all compilation errors across subsystems

3. **Week 5-6:** Phase 3 (Commands/Queries)
   - Focus: Economy subsystem (gold transfers, taxes, deposits)
   - Deliverable: Command/query infrastructure, all Economy handlers implemented
   - **Breaking:** Remove old service methods as handlers are completed
   - No compatibility wrappers; direct replacement
   - Update all call sites to use handlers via `IWorldEngine`

4. **Week 7:** Phase 4 (Event Bus)
   - Focus: Core events (gold, production, harvesting)
   - Deliverable: `IEventBus`, `InMemoryEventBus`, event publishing required for all mutations
   - **Breaking:** Command handlers must publish events (enforced in base handler)
   - Add event assertions to all integration tests
   - Wire up cross-subsystem event subscribers

5. **Week 8:** Phase 5 (Public API)
   - Focus: `IWorldEngine` façade, lock down internals
   - Deliverable: Unified API, internal access restricted, documentation
   - **Breaking:** Mark repositories and domain services as `internal`
   - External code must use `IWorldEngine` façade (compilation enforced)
   - Publish API documentation and migration examples

---

## Success Criteria

- [ ] No raw `int`/`string` primitives in public APIs for settlements, regions, coinhouses
- [ ] Organizations and coinhouses can participate in economy as first-class actors
- [ ] Command handlers validate and publish events for all mutations
- [ ] Query handlers are read-only and do not trigger side effects
- [ ] External services interact with WorldEngine only via `IWorldEngine` façade
- [ ] All existing tests pass after each phase
- [ ] New behavior-driven tests cover Persona interactions (org deposits gold, etc.)
- [ ] Event subscribers successfully react to cross-subsystem changes (e.g., tax on transfer)

---

## Decisions and Implementation Notes

### Persona Persistence (Phase 1)
**Decision:** Add `PersonaId` immediately in Phase 1 alongside strong types.
- Database migrations will add `PersonaId` columns to all relevant tables
- Keep `CharacterId` temporarily during Phase 1-2 for data migration safety
- Drop `CharacterId` columns at end of Phase 2 once all code migrated
- Use foreign key constraints during transition to maintain referential integrity

### Event Store Architecture (Phase 4)
**Decision:** Use C# Channels with background tasks for non-blocking event processing.
- **In-Game Thread:** Commands publish events to a `Channel<IDomainEvent>`
- **Background Tasks:** Consumer tasks read from channel and process events in parallel
- **Schedulers:** Use NWN scheduler or custom timer to sync game-thread-required reactions
- **Cross-Thread Communication:** Background tasks that need to mutate game state push results back via scheduler callback
- Event persistence (audit log) will be background task writing to database
- This keeps game loop non-blocking while allowing true parallelism for event processing

**Implementation Notes:**
```csharp
// Phase 4 event bus will use Channel<T>
public sealed class ChannelEventBus : IEventBus
{
    private readonly Channel<IDomainEvent> _eventChannel;
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        where TEvent : IDomainEvent
    {
        // Non-blocking write to channel
        await _eventChannel.Writer.WriteAsync(@event, ct);
    }

    // Background consumer task processes events off-thread
    private async Task ProcessEventsAsync(CancellationToken ct)
    {
        await foreach (var evt in _eventChannel.Reader.ReadAllAsync(ct))
        {
            // Process in parallel, schedule game-thread callbacks as needed
        }
    }
}
```

### Command Validation (Phase 3)
**Decision:** Validation happens in factory methods using `Parse` pattern, not in handlers.
- All value objects validate on construction (e.g., `SettlementId.Parse(int)`)
- Commands are records with `required` properties; factory methods enforce business rules
- Handlers assume valid input (fail-fast if invalid types slip through)
- No separate `IValidator<TCommand>` pipeline needed

**Example:**
```csharp
public sealed record TransferGoldCommand
{
    public required PersonaId From { get; init; }
    public required PersonaId To { get; init; }
    public required Quantity Amount { get; init; }
    public required string Reason { get; init; }

    // Factory method enforces business rules
    public static TransferGoldCommand Create(PersonaId from, PersonaId to, int amount, string reason)
    {
        if (from == to)
            throw new InvalidOperationException("Cannot transfer to self");

        return new TransferGoldCommand
        {
            From = from,
            To = to,
            Amount = Quantity.Parse(amount), // validates >= 0
            Reason = string.IsNullOrWhiteSpace(reason)
                ? throw new ArgumentException("Reason required")
                : reason
        };
    }
}
```

### API Versioning
**Decision:** No versioning strategy needed pre-deployment.
- Post-deployment, use semantic versioning (semver) on `IWorldEngine` assembly
- Breaking changes to commands/queries → major version bump
- New commands/queries/events → minor version bump
- Bug fixes in handlers → patch version bump

### Test Migration Strategy (All Phases)
**Decision:** Adapt tests to new patterns using BDD/DDD code-first approach.
- **No Gherkin:** Write tests in C# using Given-When-Then method names
- **Code-First BDD:** Test names describe behavior, not implementation
- **Delete if unmaintainable:** If a test is too coupled to old primitives, delete and rewrite from behavior spec
- **Test Helpers:** Create fluent builders for `Persona`, commands, and events in `Tests/Helpers/`

**Example Test Pattern:**
```csharp
[Test]
public void Given_ValidTransferCommand_When_Executed_Then_GoldIsTransferred_And_EventPublished()
{
    // Given
    var from = PersonaTestHelpers.CreateCharacterPersona();
    var to = PersonaTestHelpers.CreateOrganizationPersona("merchants_guild");
    var command = TransferGoldCommand.Create(from.Id, to.Id, 100, "Guild dues");

    // When
    var result = await _worldEngine.ExecuteAsync(command);

    // Then
    Assert.That(result.Success, Is.True);
    _eventBusMock.Verify(e => e.PublishAsync(
        It.Is<GoldTransferredEvent>(evt =>
            evt.From == from.Id &&
            evt.To == to.Id &&
            evt.Amount.Value == 100),
        It.IsAny<CancellationToken>()),
        Times.Once);
}
```

---

## Additional Considerations

### 1. Channel Capacity and Backpressure
- Events published faster than background tasks can process → channel fills
- **Recommendation:** Use bounded channel with capacity limit; apply backpressure policy
  - Drop oldest events (for non-critical analytics)
  - Wait/block game thread (for critical events like gold transfers)
  - Hybrid: critical events bypass channel, go to synchronous handlers

### 2. Event Ordering Guarantees
- Channel preserves FIFO order, but parallel consumers may process out-of-order
- **Recommendation:**
  - Single consumer task per event type if ordering matters (gold transfers)
  - Multiple consumer tasks for independent events (harvesting, crafting)
  - Add sequence number to events for audit/replay

### 3. NWN Scheduler Integration
- Background tasks cannot directly call NWN API (thread safety)
- **Recommendation:** Background tasks queue game-state mutations via `Scheduler.Schedule()` callback
```csharp
// In background event consumer
await Task.Run(() => {
    // Process event off-thread
    var result = ComputeIntensiveOperation();

    // Schedule game-thread callback
    Scheduler.Schedule(() => {
        nwCreature.ApplyEffect(result); // Safe on game thread
    });
});
```

### 4. Event Replay and Audit
- Persistent event log enables replay for debugging or rollback
- **Recommendation:** Phase 4 includes simple append-only event log table
  - Background task writes events to DB asynchronously
  - Indexed by `EventId`, `Actor`, `OccurredAt` for queries
  - Replay tool reads events and re-executes commands (Phase 5+)

### 5. Test Helpers Location
- Create `Tests/Helpers/WorldEngine/` subdirectory structure:
  - `PersonaTestHelpers.cs` - Factory methods for test personas
  - `CommandTestHelpers.cs` - Fluent builders for commands
  - `EventAssertions.cs` - Custom NUnit constraints for event verification

### 6. EF Core Migration Strategy
- Phase 1: Add `PersonaId` columns alongside existing `CharacterId` columns
  - EF Core migration adds nullable `PersonaId` columns to all relevant tables
  - Include data migration in `Up()` method to populate `PersonaId` from `CharacterId`

- Phase 2: Drop `CharacterId` columns after full migration
  - EF Core migration removes old columns once all code uses `PersonaId`
  - `Down()` method recreates columns and reverses data migration for rollback safety

**Example Migration Pattern:**
```csharp
// Phase 1: Add PersonaId
public partial class AddPersonaIdColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns
        migrationBuilder.AddColumn<string>(
            name: "PersonaId",
            table: "Transactions",
            type: "text",
            nullable: true);

        // Migrate existing data
        migrationBuilder.Sql(@"
            UPDATE ""Transactions""
            SET ""PersonaId"" = 'Character:' || ""CharacterId""::text
            WHERE ""CharacterId"" IS NOT NULL;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PersonaId", table: "Transactions");
    }
}

// Phase 2: Drop CharacterId (after code migration complete)
public partial class RemoveCharacterIdColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CharacterId", table: "Transactions");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Restore for rollback
        migrationBuilder.AddColumn<Guid>(
            name: "CharacterId",
            table: "Transactions",
            type: "uuid",
            nullable: true);

        // Reverse migration
        migrationBuilder.Sql(@"
            UPDATE ""Transactions""
            SET ""CharacterId"" = SUBSTRING(""PersonaId"" FROM 11)::uuid
            WHERE ""PersonaId"" LIKE 'Character:%';
        ");
    }
}
```

**Migration Files Location:** `AmiaReforged.PwEngine/Migrations/`
- Migrations are auto-generated via `dotnet ef migrations add`
- Data migration SQL goes in `Up()`/`Down()` methods
- Run manually with `dotnet ef database update`

---

## Related Documents
- `Requirements.md` (Economy subsystem, to be created after refactoring)
- `API.md` (developer guide for `IWorldEngine`, to be created in Phase 5)
- `Events.md` (catalog of all domain events, to be created in Phase 4)
- `ChannelEventBus.md` (architecture decision record for event processing, to be created in Phase 4)

---

# GUIDs, Persistence, and Performance — Research Checklist

**Context & intent**

We accept .NET-generated GUIDs as the canonical identifiers for domain entities (PersonaId, CharacterId, GovernmentId, etc.). However, a persistent database row/entity is a storage concept with lifecycle/tracking semantics that must not leak into domain models or cross domain boundaries as tracked EF entities. The domain's identifier and the database's primary key may share the same GUID value, but they are different concepts and must be treated as such.

This checklist documents research tasks, decisions to make, and practical tests to run before we lock in storage patterns.

## Goals

- Keep domain models pure: value objects, records, and strongly-typed IDs that are independent of EF Core tracking semantics.
- Store GUIDs efficiently in the database with minimal index fragmentation and good insert/read throughput.
- Avoid crossing the domain boundary with tracked DB entities (prevent accidental long-lived change tracking and memory leaks).
- Validate that GUID choices don't create unexpected performance or storage regressions for our expected scale.

---

## Research checklist (actionable)

1. Storage type and representation
   - Verify the DB type for GUIDs (Postgres: `uuid`, SQL Server: `uniqueidentifier`).
   - Avoid storing GUIDs as `varchar`/`text` where possible — prefer native `uuid`/`uniqueidentifier`/binary(16).
   - If using Postgres, confirm `uuid-ossp` or `pgcrypto` availability if DB-side generation is needed.

2. GUID generation strategy
   - Decide on generator: client-generated (Guid.NewGuid / .NET) vs DB-generated (NEWSEQUENTIALID / uuid_generate_v1mc).
   - Evaluate sequential/COMB GUIDs for clustered primary keys to reduce index fragmentation and page splits.
   - If using .NET generation, evaluate libraries for sequential GUIDs (if we need them) and weigh in against simplicity of Guid.NewGuid.

3. Indexing and primary key strategy
   - Benchmark clustered index behavior with GUID PK vs surrogate integer PK for our realistic insertion pattern.
   - If GUID is the clustered PK, test sequential GUID options to limit fragmentation.
   - Consider using non-clustered primary key with a separate clustered index (depends on DB engine) if that suits queries better.

4. EF Core mapping and patterns
   - Map domain ID value objects (e.g., CharacterId, PersonaId) to DB `uuid` columns through ValueConverters. Ensure conversions are efficient (no extra allocations).
   - Always return domain objects from repositories, not EF tracked entities. Use `AsNoTracking()` for read-only operations.
   - Avoid exposing DbContext or EF entities across application boundaries.

5. Memory/GC and tracking behaviors
   - Measure EF Core tracked memory when loading large lists of entities. Create benchmarks that simulate expected peak loads.
   - Use `AsNoTracking()` and pagination for bulk reads to avoid high memory usage.
   - Ensure tests cover the repository pattern with and without tracking to assert memory use.

6. Serialization and wire format
   - Standardize JSON serialization for GUIDs (string canonical form). Add converters for value-object IDs.
   - Ensure round-trip conversion from domain ID -> DB -> domain ID is lossless.

7. Collision risk and uniqueness
   - Document collision probability (practically zero for our scale). No need to rotate GUID algorithm for uniqueness concerns.
   - Verify constraints at DB layer (unique constraints) remain in place.

8. Migration and operational concerns
   - If converting existing numeric/sequential PKs to GUIDs, prototype migration SQL/scripts in `Migrations/Data/` (idempotent with rollback).
   - Test migration on realistic sized dataset and measure downtime, index rebuild costs, and replication lag (if applicable).

9. Performance benchmarks (must run)
   - Insert throughput: measure inserts/sec with GUID PK (random) vs sequential GUID vs integer PK.
   - Read throughput: measure lookup speed by PK and by indexed foreign keys (joins using GUIDs).
   - Index size: compare index storage sizes for `uuid` vs `int` primary keys for a simulated dataset.
   - EF Core allocations: measure per-entity allocation when mapping GUID-backed value objects.

10. Application design rules (non-technical but enforceable)
    - Repositories return pure domain value objects; never leak EF change-tracked entities to services.
    - Use factories or mapping layers to convert DB rows to domain objects at repository boundaries.
    - Keep domain IDs as small, immutable structs/records (value objects) and avoid wrapping them in reference types unnecessarily.
    - Where possible, use streaming/pagination for large result sets and assert `AsNoTracking()` on read-only routes.

11. Test-suite updates
    - Add unit tests that assert ValueConverter behavior between `PersonaId` (value object) and DB `uuid` column.
    - Add integration tests for repository behaviors with and without `AsNoTracking()` to verify memory usage and correctness.

12. Operational monitoring
    - Add metrics around insert latency, index bloat, and table/index sizes to our monitoring dashboard for early detection.
    - Track the size of EF Core change tracker during integration tests and load tests as a metric to avoid surprises.

---

## Practical recommendations (opinionated)

- Use .NET-generated GUIDs (Guid.NewGuid()) for simplicity unless insert throughput tests show contention from random GUIDs.
- Store GUIDs as native `uuid`/`uniqueidentifier` in the DB; avoid string columns for identifiers.
- Use a sequential/COMB variant only if the insertion benchmark shows index fragmentation affecting latency or storage.
- Keep domain ID types as value objects (records/structs) and map them with EF Core ValueConverters. Repositories should materialize domain objects and never hand out tracked EF entities.
- Use `AsNoTracking()` for read-only queries and pagination for large lists to keep memory bounded.
- For this project's expected scale and our available hardware (64 GB RAM), the default GUID approach is safe; however, complete the benchmarks above before optimizing prematurely.

---

## Acceptance criteria for this research

- [ ] Benchmarks for insert/read/index-size completed and documented (short report attached to `Refactoring.md`).
- [ ] EF Core mapping tests for GUID value objects pass.
- [ ] Migration scripts (if needed) prepared under `Migrations/Data/` and tested on a snapshot DB.
- [ ] A short implementation guideline added to `README.md` or `API.md` describing repository boundaries and domain/DB separation.

