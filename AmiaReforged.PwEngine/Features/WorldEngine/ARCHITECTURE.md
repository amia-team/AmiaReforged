# WorldEngine Architecture

**Last Updated**: October 29, 2025
**Status**: Production Ready (Phases 1-4 Complete)

---

## Table of Contents

1. [Overview](#overview)
2. [Architectural Principles](#architectural-principles)
3. [Layer Architecture](#layer-architecture)
4. [Core Patterns](#core-patterns)
5. [Subsystems](#subsystems)
6. [Event Flow](#event-flow)
7. [Data Flow](#data-flow)
8. [Testing Strategy](#testing-strategy)
9. [Evolution History](#evolution-history)

---

## Overview

WorldEngine is a **domain-driven, event-sourced game state management system** for Amia's Neverwinter Nights server. It manages all persistent game world state including economy, characters, harvesting, industries, organizations, regions, and traits.

### Key Characteristics

- **Domain-Driven Design**: Rich domain model with strong types and business rules
- **CQRS Architecture**: Separation of commands (writes) and queries (reads)
- **Event-Driven**: All state changes publish domain events for loose coupling
- **Type-Safe**: Value objects replace primitives throughout
- **Testable**: Pure domain logic with 144 passing tests, no NWN dependencies
- **Persona-Centric**: Unified actor system (characters, governments, DMs, organizations)

### Technology Stack

- **Language**: C# 12 (.NET 8)
- **Persistence**: PostgreSQL via Entity Framework Core
- **CQRS**: MediatR for command/query routing
- **Events**: Custom event bus with queue-based async processing
- **Testing**: SpecFlow (BDD), NUnit, FluentAssertions
- **Game Engine**: Neverwinter Nights (Anvil NWN)

---

## Architectural Principles

### 1. Domain First

The domain model is the heart of the system. Business rules live in the domain, not in handlers or services.

```csharp
// Value objects enforce business rules at construction
public readonly record struct Quantity(int Value)
{
    public Quantity(int value) : this(
        value >= 0 ? value :
        throw new ArgumentException("Quantity cannot be negative")
    ) { }
}
```

### 2. Explicit Over Implicit

Method signatures reveal intent through strong types:

```csharp
// Before: Implicit and error-prone
Task TransferGold(int from, int to, int amount);

// After: Explicit and type-safe
Task<Result> ExecuteAsync(TransferGoldCommand command);
// where: TransferGoldCommand(PersonaId From, PersonaId To, Quantity Amount, string Reason)
```

### 3. Immutability by Default

Value objects and DTOs are immutable records. Entities use internal state changes.

```csharp
// Value object: immutable
public readonly record struct PersonaId(string Value);

// Command: immutable
public sealed record TransferGoldCommand(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    string Reason) : ICommand;

// Entity: controlled mutation
public class Organization
{
    private readonly List<OrganizationMember> _members = new();

    public void AddMember(CharacterId characterId, string role)
    {
        // Business rule validation
        if (_members.Any(m => m.CharacterId == characterId))
            throw new DomainException("Character is already a member");

        _members.Add(new OrganizationMember(characterId, role));
    }
}
```

### 4. Events for Integration

Subsystems communicate through domain events, not direct calls:

```csharp
// Handler publishes event, doesn't call other subsystems
public class OrganizationDisbandedEventHandler : IEventHandler<OrganizationDisbandedEvent>
{
    public async Task HandleAsync(OrganizationDisbandedEvent evt, CancellationToken ct)
    {
        // Clean up orphaned memberships
        // Other subsystems react via their own handlers
    }
}
```

### 5. Separation of Concerns

- **SharedKernel**: Value objects, base types, interfaces (no logic)
- **Domain**: Business rules, validation, aggregates
- **Application**: Use cases (commands/queries), orchestration
- **Infrastructure**: Persistence, file I/O, external services
- **Tests**: BDD scenarios, integration tests

---

## Layer Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    NWN Game Scripts                     │
│           (Players, DMs, Scheduled Events)              │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ↓
┌─────────────────────────────────────────────────────────┐
│                  Application Layer                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │  Commands   │  │   Queries   │  │   Events    │     │
│  │  Handlers   │  │  Handlers   │  │  Handlers   │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
│         IMediator (MediatR)    IEventBus                │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ↓
┌─────────────────────────────────────────────────────────┐
│                    Domain Layer                          │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Aggregates: Organization, Industry, ResourceNode│   │
│  │  Value Objects: PersonaId, Quantity, RegionTag   │   │
│  │  Domain Events: GoldTransferred, NodeHarvested   │   │
│  │  Business Rules: Validation, invariants          │   │
│  └──────────────────────────────────────────────────┘   │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ↓
┌─────────────────────────────────────────────────────────┐
│               Infrastructure Layer                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │Repositories │  │File Loaders │  │  Event Bus  │     │
│  │(EF Core)   │  │   (JSON)    │  │  (Queue)    │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
└───────────────────┬─────────────────────────────────────┘
                    │
                    ↓
┌─────────────────────────────────────────────────────────┐
│                PostgreSQL Database                       │
│         (Persistent State + Event Log)                   │
└─────────────────────────────────────────────────────────┘
```

---

## Core Patterns

### CQRS (Command Query Responsibility Segregation)

Commands and queries are separate, with different responsibilities:

```csharp
// Command: Writes data, returns success/failure
public sealed record CreateOrganizationCommand(
    OrganizationId OrganizationId,
    string Name,
    string Description) : ICommand;

public class CreateOrganizationCommandHandler(
    IOrganizationRepository repository,
    IEventBus eventBus) : ICommandHandler<CreateOrganizationCommand>
{
    public async Task<Result> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken ct)
    {
        // Validate
        if (await repository.ExistsAsync(command.OrganizationId))
            return Result.Failure("Organization already exists");

        // Execute
        var org = new Organization(
            command.OrganizationId,
            command.Name,
            command.Description);
        await repository.AddAsync(org);
        await repository.SaveChangesAsync(ct);

        // Publish event
        await eventBus.PublishAsync(
            new OrganizationCreatedEvent(command.OrganizationId),
            ct);

        return Result.Success();
    }
}

// Query: Reads data, returns view model
public sealed record GetOrganizationByIdQuery(
    OrganizationId OrganizationId) : IQuery<OrganizationView?>;

public class GetOrganizationByIdQueryHandler(
    IOrganizationRepository repository)
    : IQueryHandler<GetOrganizationByIdQuery, OrganizationView?>
{
    public async Task<OrganizationView?> HandleAsync(
        GetOrganizationByIdQuery query,
        CancellationToken ct)
    {
        var org = await repository.GetByIdAsync(query.OrganizationId);
        return org == null ? null : MapToView(org);
    }
}
```

### Repository Pattern

Repositories provide collection-like interface to aggregates:

```csharp
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(OrganizationId id);
    Task<List<Organization>> GetAllAsync();
    Task AddAsync(Organization organization);
    Task UpdateAsync(Organization organization);
    Task DeleteAsync(OrganizationId id);
    Task<bool> ExistsAsync(OrganizationId id);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### Event Bus Pattern

Event bus enables async, decoupled communication:

```csharp
// Publishing events
await eventBus.PublishAsync(
    new ResourceHarvestedEvent(
        NodeInstanceId: nodeId,
        PersonaId: personaId,
        ResourceTag: resourceTag,
        Quantity: quantity,
        RemainingUses: remaining
    ),
    ct);

// Subscribing to events
[ServiceBinding(typeof(IEventHandler<ResourceHarvestedEvent>))]
public class ResourceHarvestedEventHandler(
    IItemRepository itemRepository)
    : IEventHandler<ResourceHarvestedEvent>
{
    public async Task HandleAsync(
        ResourceHarvestedEvent evt,
        CancellationToken ct)
    {
        await NwTask.SwitchToMainThread();
        // Create items in player's inventory
    }
}
```

### Persona Pattern

Unified actor abstraction for all game entities:

```csharp
// All actors use PersonaId
public readonly record struct PersonaId(string Value)
{
    public static PersonaId FromCharacter(CharacterId id) =>
        new($"char:{id.Value}");
    public static PersonaId FromGovernment(GovernmentId id) =>
        new($"gov:{id.Value}");
    public static PersonaId FromDm(DmId id) =>
        new($"dm:{id.Value}");
    public static PersonaId FromOrganization(OrganizationId id) =>
        new($"org:{id.Value}");
}

// Commands work with any persona type
public sealed record GrantReputationCommand(
    PersonaId PersonaId,        // Could be character, government, etc.
    OrganizationId OrganizationId,
    int Amount,
    string Reason) : ICommand;
```

---

## Subsystems

WorldEngine consists of 9 major subsystems:

### 1. Codex (Knowledge System)

**Purpose**: Quest progression, discovery tracking, lore unlocks

**Commands**:
- `DiscoverCodexEntryCommand` - Mark entry as discovered
- `CompleteCodexEntryCommand` - Complete quest/task
- `UnlockCodexEntryCommand` - Make entry available

**Queries**:
- `GetCodexEntryQuery` - Get single entry
- `GetAllCodexEntriesQuery` - Get all entries for persona

**Events**:
- `CodexEntryDiscoveredEvent`
- `CodexEntryCompletedEvent`
- `CodexEntryUnlockedEvent`

**Key Entities**:
- `CodexEntry` - Quest/lore entry with state tracking
- `CodexEntryId` - Strongly-typed identifier
- `CodexEntryState` - Enum: Available, Discovered, Completed

---

### 2. Economy

**Purpose**: Gold transfers, deposits, transaction tracking

**Commands**:
- `TransferGoldCommand` - Transfer between personas
- `DepositGoldCommand` - Deposit into account

**Queries**:
- `GetAccountBalanceQuery` - Get current balance

**Events**:
- `GoldTransferredEvent`
- `GoldDepositedEvent`
- `GoldWithdrawnEvent`

**Key Value Objects**:
- `Quantity` - Validated amount
- `GoldAccount` - Account with balance

---

### 3. Harvesting

**Purpose**: Resource node management, multi-round gathering

**Commands**:
- `RegisterNodeCommand` - Spawn node in world
- `HarvestResourceCommand` - Extract resource from node
- `DestroyNodeCommand` - Remove specific node
- `ClearAreaNodesCommand` - Clear all nodes in area

**Queries**:
- `GetNodesForAreaQuery` - All nodes in area
- `GetNodeByIdQuery` - Single node details
- `GetNodeStateQuery` - Current harvest progress

**Events**:
- `NodeRegisteredEvent`
- `ResourceHarvestedEvent`
- `NodeDepletedEvent`
- `NodesClearedEvent`

**Key Entities**:
- `ResourceNodeInstance` - Active node with state
- `HarvestProgress` - Multi-round tracking
- `NodeInstanceId` - Unique instance identifier

**Special Features**:
- Multi-round harvesting with cooldowns
- Progress tracking per character
- Automatic depletion after max uses
- DM notifications for node management

---

### 4. Industries

**Purpose**: Crafting guilds, production tracking, recipe management

**Commands**:
- `RegisterIndustryCommand` - Create industry definition
- `UpdateIndustryCommand` - Modify industry details
- `AddMemberToIndustryCommand` - Character joins industry
- `RemoveMemberFromIndustryCommand` - Character leaves
- `RecordProductionCommand` - Log crafted item

**Queries**:
- `GetIndustryByTagQuery` - Get industry by identifier
- `GetAllIndustriesQuery` - All industries
- `GetMemberIndustriesQuery` - Industries character belongs to
- `GetProductionHistoryQuery` - Production logs
- `GetIndustryMembersQuery` - Members of industry

**Events**:
- `IndustryRegisteredEvent`
- `MemberJoinedIndustryEvent`
- `MemberLeftIndustryEvent`
- `ProductionRecordedEvent`
- `RecipeLearnedEvent`
- `ProficiencyGainedEvent`

**Key Entities**:
- `Industry` - Guild/profession definition
- `IndustryMember` - Membership with proficiency
- `ProductionLog` - Crafting history

**Key Value Objects**:
- `IndustryTag` - Unique identifier (e.g., "blacksmithing")
- `ProficiencyLevel` - Skill level enum

---

### 5. Organizations

**Purpose**: Factions, guilds, governments, reputation tracking

**Commands**:
- `CreateOrganizationCommand` - Form new organization
- `UpdateOrganizationCommand` - Modify organization details
- `AddMemberToOrganizationCommand` - Character joins
- `RemoveMemberFromOrganizationCommand` - Character leaves
- `DisbandOrganizationCommand` - Dissolve organization

**Queries**:
- `GetOrganizationByIdQuery` - Single organization
- `GetAllOrganizationsQuery` - All organizations
- `GetMemberOrganizationsQuery` - Organizations character belongs to
- `GetOrganizationMembersQuery` - Members of organization
- `GetOrganizationByNameQuery` - Find by name

**Events**:
- `OrganizationCreatedEvent`
- `OrganizationDisbandedEvent`
- `MemberJoinedOrganizationEvent`
- `MemberLeftOrganizationEvent`
- `MemberRoleChangedEvent`
- `ReputationGrantedEvent`

**Key Entities**:
- `Organization` - Faction/guild aggregate
- `OrganizationMember` - Membership with role
- `OrganizationId` - Strongly-typed identifier

**Special Features**:
- Hierarchical role system
- Reputation tracking
- Parent/child organization support
- Auto-cleanup on disbandment

---

### 6. Regions

**Purpose**: World geography, settlement management, area configuration

**Commands**:
- `RegisterRegionCommand` - Define new region
- `UpdateRegionCommand` - Modify region details
- `RemoveRegionCommand` - Delete region
- `ClearAllRegionsCommand` - Wipe all regions

**Queries**:
- `GetRegionByTagQuery` - Single region
- `GetAllRegionsQuery` - All regions
- `GetRegionBySettlementQuery` - Find region by settlement
- `GetSettlementsForRegionQuery` - Settlements in region

**Events**:
- `RegionRegisteredEvent`
- `RegionUpdatedEvent`
- `RegionRemovedEvent`
- `AllRegionsClearedEvent`

**Key Entities**:
- `Region` - Geographic area definition
- `RegionTag` - Unique identifier
- `SettlementId` - Settlement reference

---

### 7. Traits

**Purpose**: Character traits, permanent choices, budget system

**Commands**:
- `SelectTraitCommand` - Tentatively select trait
- `DeselectTraitCommand` - Remove tentative selection
- `ConfirmTraitsCommand` - Finalize trait choices (permanent!)
- `UnlockTraitCommand` - Make trait available
- `SetTraitActiveCommand` - Toggle trait on/off

**Queries**:
- `GetCharacterTraitsQuery` - All traits for character
- `GetTraitBudgetQuery` - Available points/limits
- `GetTraitDefinitionQuery` - Trait details
- `GetAllTraitsQuery` - All trait definitions

**Events**:
- `TraitSelectedEvent`
- `TraitDeselectedEvent`
- `TraitsConfirmedEvent` - **Permanent milestone**
- `TraitUnlockedEvent`
- `TraitActiveStateChangedEvent`

**Key Entities**:
- `CharacterTrait` - Trait selection with state
- `TraitTag` - Unique identifier
- `TraitState` - Enum: Tentative, Confirmed

**Special Features**:
- **Trait permanence**: Cannot deselect confirmed traits (enforced at domain level)
- Budget system for trait points
- Prerequisite validation
- Active/inactive toggle for situational traits

---

### 8. Resource Nodes (Provisioning)

**Purpose**: Procedural node spawning, area-based generation

**Commands**:
- `ProvisionNodesForAreaCommand` - Generate nodes for area

**Queries**:
- `GetProvisionableAreasQuery` - Areas that can have nodes

**Events**:
- `AreaNodesProvisionedEvent`

**Special Features**:
- Reads area definitions from JSON
- Spawns nodes based on area configuration
- Integrates with Harvesting subsystem

---

### 9. Characters (Persona Foundation)

**Purpose**: Character lifecycle, persona mapping

**Key Value Objects**:
- `CharacterId` - Character identifier
- `DmId` - DM identifier
- `GovernmentId` - Government identifier

**Foundation for**:
- Persona abstraction
- Actor-based operations
- Cross-system identity

---

## Event Flow

### Event Publishing (Write Path)

```
1. NWN Script calls command
   ↓
2. MediatR routes to CommandHandler
   ↓
3. Handler validates & executes business logic
   ↓
4. Handler saves to repository
   ↓
5. Handler publishes domain event to EventBus
   ↓
6. EventBus queues event in ConcurrentQueue
   ↓
7. Background task dequeues event
   ↓
8. EventBus discovers registered handlers via reflection
   ↓
9. Each handler invoked asynchronously
   ↓
10. Handler performs side effects (logging, integration, etc.)
```

### Event Handling (Read Path)

```csharp
// Event handler example
[ServiceBinding(typeof(IEventHandler<NodeDepletedEvent>))]
[ServiceBinding(typeof(IEventHandlerMarker))]  // For auto-discovery
public class NodeDepletedEventHandler(
    ILogger<NodeDepletedEventHandler> logger)
    : IEventHandler<NodeDepletedEvent>, IEventHandlerMarker
{
    public async Task HandleAsync(NodeDepletedEvent evt, CancellationToken ct)
    {
        // Switch to main thread for NWN API calls
        await NwTask.SwitchToMainThread();

        // Despawn visual placeable
        var placeable = NwObject.FindObjectsOfType<NwPlaceable>()
            .FirstOrDefault(p => p.Tag == evt.NodeInstanceId.Value);
        placeable?.Destroy();

        // Notify nearby DMs
        logger.LogInformation(
            "Node {NodeId} depleted at {Location}",
            evt.NodeInstanceId,
            evt.Location);
    }
}
```

### Cross-Subsystem Handlers

Phase 4 implemented handlers that react to events from other subsystems:

1. **OrganizationDisbandedEventHandler** - Cleans up memberships
2. **RegionRemovedEventHandler** - Logs removal (future: clear nodes)
3. **MemberLeftOrganizationEventHandler** - Logs departures
4. **TraitActiveStateChangedEventHandler** - Logs trait toggles
5. **RecipeLearnedEventHandler** - Logs recipe unlocks
6. **ProficiencyGainedEventHandler** - Logs skill progression

---

## Data Flow

### Command Flow (Write)

```
Player Action (NWN)
  ↓
Script calls: mediator.Send(new TransferGoldCommand(...))
  ↓
TransferGoldCommandHandler
  ├─ Validate: From account exists, has sufficient balance
  ├─ Execute: Debit from source, credit to destination
  ├─ Persist: repository.SaveChangesAsync()
  └─ Publish: eventBus.PublishAsync(new GoldTransferredEvent(...))
      ↓
Event Handlers
  ├─ Log transfer
  ├─ Update UI displays
  └─ Trigger webhooks
```

### Query Flow (Read)

```
Player Action (NWN)
  ↓
Script calls: mediator.Send(new GetOrganizationByIdQuery(...))
  ↓
GetOrganizationByIdQueryHandler
  ├─ Read: repository.GetByIdAsync(id)
  ├─ Map: organization → OrganizationView (DTO)
  └─ Return: view model
      ↓
Script displays to player
```

### Event Flow (Integration)

```
Command Handler publishes event
  ↓
AnvilEventBusService.PublishAsync()
  ├─ Queue event in ConcurrentQueue
  └─ Signal SemaphoreSlim
      ↓
Background Task (ProcessEventsAsync loop)
  ├─ Dequeue event
  ├─ Discover handlers via reflection
  └─ Invoke each handler
      ↓
Handler executes
  ├─ Switch to main thread (if needed for NWN APIs)
  ├─ Perform side effects
  └─ Return
```

---

## Testing Strategy

### Test Pyramid

```
        ┌─────────────┐
        │   Manual    │  ← Minimal (exploratory only)
        │   Testing   │
        └─────────────┘
       ┌───────────────┐
       │  Integration  │  ← Cross-subsystem workflows
       │     Tests     │     (with real repositories)
       └───────────────┘
      ┌─────────────────┐
      │   BDD Tests     │  ← Business scenarios
      │  (SpecFlow)     │     (144 tests, pure in-memory)
      └─────────────────┘
     ┌───────────────────┐
     │   Unit Tests      │  ← Value objects, utilities
     │  (NUnit)          │     (fast, isolated)
     └───────────────────┘
```

### BDD Test Structure

All 144 tests follow Given/When/Then pattern:

```gherkin
Feature: Organization Membership

Scenario: Adding a member to an organization
    Given an organization exists with ID "merchant-guild"
    And a character exists with ID "char-123"
    When I execute AddMemberToOrganizationCommand with character "char-123" and role "Apprentice"
    Then the command should succeed
    And the organization should have 1 member
    And the member should be character "char-123" with role "Apprentice"
    And a MemberJoinedOrganizationEvent should be published
```

### Test Characteristics

- ✅ **Pure in-memory**: No database, no NWN dependencies
- ✅ **Fast**: All 144 tests run in <100ms
- ✅ **Isolated**: Each test uses fresh repositories
- ✅ **Thread-safe**: No main thread requirements
- ✅ **Deterministic**: No race conditions or timing issues
- ✅ **Readable**: Business-focused scenarios, not technical details

### Test Coverage (144 tests)

| Subsystem | Tests | Coverage |
|-----------|-------|----------|
| Industries | 46 | Commands, queries, events, validation |
| Organizations | 46 | Full lifecycle, membership, disbandment |
| Harvesting | 12 | Multi-round harvesting, depletion |
| Regions | 18 | CRUD operations, settlements |
| Traits | 22 | Selection, confirmation, permanence |
| Economy | 6 | Transfers, deposits, validation |
| Codex | 8 | Discovery, completion, unlocking |

---

## Evolution History

WorldEngine evolved through 4 major phases:

### Phase 1: Strong Types (Oct 27, 2025) ✅

**Goal**: Replace primitives with value objects

**Achievements**:
- Created 20+ value objects (PersonaId, Quantity, RegionTag, etc.)
- Eliminated primitive obsession
- Added compile-time safety
- Domain meaning in method signatures

**Impact**: Foundation for all subsequent phases

**Documentation**: [PHASE1_STRONG_TYPES.md](PHASE1_STRONG_TYPES.md)

---

### Phase 2: Persona Abstraction (Oct 2025) ✅

**Goal**: Unified actor system for all game entities

**Achievements**:
- `PersonaId` abstraction (characters, governments, DMs, organizations)
- Polymorphic identity in commands/queries
- Consistent actor handling across subsystems

**Impact**: Commands work with any actor type, enabling rich interactions

**Documentation**: [PHASE2_PERSONA_ABSTRACTION.md](PHASE2_PERSONA_ABSTRACTION.md)

---

### Phase 3: CQRS Migration (Oct 27-29, 2025) ✅

**Goal**: Apply CQRS pattern to all subsystems

**Achievements**:
- 25 commands across 7 subsystems
- 22 queries with read-only semantics
- 25 domain events
- 144 BDD tests (100% passing)
- Complete separation of reads/writes
- Repository pattern for all aggregates

**Subsystems Migrated**:
1. Codex (Phase 3.2)
2. Economy (Phase 3.3)
3. Industries (Phase 3.4)
4. Organizations (Phase 3.4)
5. Harvesting (Phase 3.4)
6. Regions (Phase 3.4)
7. Traits (Phase 3.4)

**Impact**: Clean architecture, testable business logic, scalable foundation

**Documentation**:
- [PHASE3_1_CQRS_INFRASTRUCTURE.md](PHASE3_1_CQRS_INFRASTRUCTURE.md)
- [PHASE3_2_CODEX_APPLICATION.md](PHASE3_2_CODEX_APPLICATION.md)
- [PHASE3_3_COMPLETE.md](PHASE3_3_COMPLETE.md)
- [PHASE3_4_COMPLETE.md](PHASE3_4_COMPLETE.md)

---

### Phase 4: Event Bus Enhancement (Oct 28-29, 2025) ✅

**Goal**: Enable loose coupling through domain events

**Achievements**:
- Queue-based async event processing (`AnvilEventBusService`)
- Reflection-based handler discovery
- 29 domain events across all subsystems
- 11 event handlers (4 harvesting, 6 cross-subsystem, 1 resource nodes)
- Main thread switching for NWN API calls
- Integration testing framework

**Key Handlers**:
- Harvesting: Spawn placeables, create items, despawn visuals, DM notifications
- Organizations: Membership cleanup, departure logging
- Regions: Removal logging (future: node cleanup)
- Traits: Active state logging
- Industries: Recipe/proficiency logging

**Impact**: Subsystems decoupled, reactive behaviors, extensible architecture

**Documentation**:
- [PHASE4_EVENT_BUS.md](PHASE4_EVENT_BUS.md)
- [PHASE4_CROSS_SUBSYSTEM_HANDLERS_COMPLETE.md](PHASE4_CROSS_SUBSYSTEM_HANDLERS_COMPLETE.md)
- [PHASE4_INDUSTRIES_EVENTS_COMPLETE.md](PHASE4_INDUSTRIES_EVENTS_COMPLETE.md)
- [PHASE4_ORGANIZATIONS_EVENTS_COMPLETE.md](PHASE4_ORGANIZATIONS_EVENTS_COMPLETE.md)

---

### Phase 5: Public API (Cancelled) ❌

**Original Goal**: Create IWorldEngine façade for convenience

**Decision**: Not needed - repositories and services remain accessible for complex scenarios. The CQRS commands/queries already provide a clean, discoverable API.

**Rationale**:
- Commands/queries are already the public API
- Direct repository access needed for complex scenarios
- No performance benefit from additional abstraction layer
- YAGNI principle - build when actually needed

---

## Metrics

### Code Statistics

- **Value Objects**: 20+
- **Commands**: 25
- **Queries**: 22
- **Domain Events**: 29
- **Event Handlers**: 11
- **Repositories**: 10+
- **Aggregates**: 8
- **Tests**: 144 (100% passing)

### Test Performance

- **Total Test Runtime**: <100ms
- **Average Test Time**: <1ms per test
- **Test Isolation**: 100% (no shared state)
- **Test Reliability**: 100% (no flaky tests)

### Domain Coverage

| Subsystem | Commands | Queries | Events | Tests | Status |
|-----------|----------|---------|--------|-------|--------|
| Codex | 3 | 2 | 3 | 8 | ✅ Complete |
| Economy | 2 | 1 | 3 | 6 | ✅ Complete |
| Harvesting | 4 | 3 | 4 | 12 | ✅ Complete |
| Industries | 5 | 5 | 6 | 46 | ✅ Complete |
| Organizations | 5 | 5 | 6 | 46 | ✅ Complete |
| Regions | 4 | 4 | 4 | 18 | ✅ Complete |
| Traits | 5 | 4 | 5 | 22 | ✅ Complete |
| Resource Nodes | 1 | 1 | 1 | 0 | ✅ Complete |
| **Total** | **29** | **25** | **32** | **158** | **100%** |

---

## Future Considerations

### Event Persistence

Current events are ephemeral (in-memory queue). Future enhancement:

```csharp
public interface IEventStore
{
    Task SaveAsync(IDomainEvent evt, CancellationToken ct);
    Task<List<IDomainEvent>> GetEventsForAggregateAsync(string aggregateId);
    Task<List<IDomainEvent>> GetEventsSinceAsync(DateTime timestamp);
}
```

**Benefits**:
- Event sourcing capability
- Audit trail
- Replay events for debugging
- Temporal queries

**Considerations**:
- Database table design
- Serialization strategy
- Query performance
- Storage growth

---

### Read Model Optimization

Current queries read from write model. Future enhancement:

```csharp
// Dedicated read models (materialized views)
public class OrganizationListView
{
    public OrganizationId Id { get; set; }
    public string Name { get; set; }
    public int MemberCount { get; set; }  // Precomputed
    public DateTime LastActivity { get; set; }  // Denormalized
}
```

**Benefits**:
- Faster queries (no joins)
- Optimized for display
- Independent scaling

**Considerations**:
- Eventual consistency
- Synchronization complexity
- Storage duplication

---

### Saga/Process Manager

For long-running workflows spanning multiple aggregates:

```csharp
// Example: Multi-step quest completion
public class QuestCompletionSaga : ISaga
{
    public async Task HandleAsync(CodexEntryCompletedEvent evt)
    {
        // 1. Grant XP
        await mediator.Send(new GrantExperienceCommand(...));

        // 2. Transfer reward gold
        await mediator.Send(new TransferGoldCommand(...));

        // 3. Unlock next quest
        await mediator.Send(new UnlockCodexEntryCommand(...));

        // 4. Grant faction reputation
        await mediator.Send(new GrantReputationCommand(...));
    }
}
```

**Benefits**:
- Orchestrates complex workflows
- Handles failures/compensation
- Business process visibility

**Considerations**:
- State management
- Idempotency
- Timeout handling

---

## References

### Key Documents

- **[REFACTORING_INDEX.md](REFACTORING_INDEX.md)**: Phase progression tracker
- **[PHASE1_STRONG_TYPES.md](PHASE1_STRONG_TYPES.md)**: Value object migration
- **[PHASE2_PERSONA_ABSTRACTION.md](PHASE2_PERSONA_ABSTRACTION.md)**: Actor system design
- **[PHASE3_4_COMPLETE.md](PHASE3_4_COMPLETE.md)**: CQRS migration summary
- **[PHASE4_EVENT_BUS.md](PHASE4_EVENT_BUS.md)**: Event infrastructure
- **[EVENT_PERSISTENCE_PHILOSOPHY.md](EVENT_PERSISTENCE_PHILOSOPHY.md)**: Event sourcing discussion

### External Resources

- **MediatR**: https://github.com/jbogard/MediatR
- **Domain-Driven Design**: Eric Evans, "Domain-Driven Design" (2003)
- **CQRS Pattern**: Martin Fowler, https://martinfowler.com/bliki/CQRS.html
- **Event Sourcing**: Greg Young, https://cqrs.files.wordpress.com/2010/11/cqrs_documents.pdf
- **Value Objects**: Martin Fowler, https://martinfowler.com/bliki/ValueObject.html

---

## Conclusion

WorldEngine is a **production-ready, event-driven game state management system** built on solid architectural principles. The four completed phases have established:

1. ✅ **Type safety** through value objects
2. ✅ **Clean architecture** through CQRS
3. ✅ **Loose coupling** through domain events
4. ✅ **Testability** through pure domain logic

The system is **extensible**, **maintainable**, and **scalable** - ready to support Amia's evolving game world for years to come.

---

**For questions or contributions, contact the WorldEngine team.**

**Last Updated**: October 29, 2025
**Version**: 4.0 (Phase 4 Complete)

