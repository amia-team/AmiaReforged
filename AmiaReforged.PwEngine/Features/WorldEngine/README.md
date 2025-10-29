# WorldEngine

**Version**: 4.0
**Status**: ✅ Production Ready
**Last Updated**: October 29, 2025

---

## What is WorldEngine?

WorldEngine is the **domain-driven, event-sourced game state management system** for Amia's Neverwinter Nights server. It manages all persistent game world state including economy, characters, harvesting, industries, organizations, regions, and traits.

---

## Quick Start

### For Developers New to WorldEngine

**Start here**: [ARCHITECTURE.md](ARCHITECTURE.md) - Comprehensive architecture guide

---

## Architecture at a Glance

```
┌─────────────────────────────────────────┐
│         NWN Game Scripts                │
│   (Players, DMs, Events)                │
└────────────┬────────────────────────────┘
             │
             ↓
┌─────────────────────────────────────────┐
│      Application Layer (CQRS)           │
│  Commands → Queries → Events            │
│      (MediatR + EventBus)               │
└────────────┬────────────────────────────┘
             │
             ↓
┌─────────────────────────────────────────┐
│         Domain Layer                    │
│  Value Objects + Aggregates             │
│  Business Rules + Validation            │
└────────────┬────────────────────────────┘
             │
             ↓
┌─────────────────────────────────────────┐
│    Infrastructure Layer                 │
│  Repositories + Event Bus + File I/O    │
└────────────┬────────────────────────────┘
             │
             ↓
┌─────────────────────────────────────────┐
│       PostgreSQL Database               │
└─────────────────────────────────────────┘
```

---

## Key Patterns

### CQRS (Command Query Responsibility Segregation)

**Commands** (writes):
```csharp
var command = new TransferGoldCommand(
    From: PersonaId.FromCharacter(playerId),
    To: PersonaId.FromGovernment(govId),
    Amount: Quantity.Parse(100),
    Reason: "Tax payment"
);
await mediator.Send(command);
```

**Queries** (reads):
```csharp
var query = new GetOrganizationByIdQuery(orgId);
var org = await mediator.Send(query);
```

### Event-Driven Integration

```csharp
// Commands publish events
await eventBus.PublishAsync(new OrganizationDisbandedEvent(orgId));

// Handlers react to events
[ServiceBinding(typeof(IEventHandler<OrganizationDisbandedEvent>))]
public class CleanupHandler : IEventHandler<OrganizationDisbandedEvent>
{
    public async Task HandleAsync(OrganizationDisbandedEvent evt, CancellationToken ct)
    {
        // Clean up memberships, notify players, etc.
    }
}
```

### Strong Types (Value Objects)

```csharp
// Before: Primitives everywhere
Task Transfer(int from, int to, int amount);

// After: Strong types with validation
Task Transfer(PersonaId from, PersonaId to, Quantity amount);
```

---

## Subsystems

| Subsystem | Purpose | Commands | Queries | Events |
|-----------|---------|----------|---------|--------|
| **Codex** | Quest progression, lore unlocks | 3 | 2 | 3 |
| **Economy** | Gold transfers, accounts | 2 | 1 | 3 |
| **Harvesting** | Resource nodes, gathering | 4 | 3 | 4 |
| **Industries** | Crafting guilds, production | 5 | 5 | 6 |
| **Organizations** | Factions, reputation | 5 | 5 | 6 |
| **Regions** | World geography, settlements | 4 | 4 | 4 |
| **Traits** | Character traits, budgets | 5 | 4 | 5 |
| **Resource Nodes** | Node provisioning | 1 | 1 | 1 |

**Total**: 29 commands, 25 queries, 32 events

---

## Testing

### Statistics
- **144 BDD tests** (100% passing)
- **<100ms** total runtime
- **Pure in-memory** (no NWN dependencies)
- **Thread-safe** (no main thread requirements)

### Example Test
```gherkin
Feature: Organization Membership

Scenario: Adding a member to an organization
    Given an organization exists with ID "merchant-guild"
    And a character exists with ID "char-123"
    When I execute AddMemberToOrganizationCommand with character "char-123"
    Then the command should succeed
    And the organization should have 1 member
    And a MemberJoinedOrganizationEvent should be published
```

---

## Project Structure

```
WorldEngine/
├── ARCHITECTURE.md                  ← Start here for architecture
├── IMPLEMENTATION_COMPLETE.md       ← Executive summary
├── REFACTORING_INDEX.md            ← Phase progression
├── SharedKernel/                   ← Value objects, base types
│   ├── ValueObjects/               ← Quantity, RegionTag, etc.
│   ├── Commands/                   ← ICommand interface
│   ├── Queries/                    ← IQuery<T> interface
│   └── Events/                     ← IDomainEvent interface
├── Application/                    ← Use cases (CQRS handlers)
│   ├── Organizations/
│   ├── Industries/
│   ├── Regions/
│   └── Traits/
├── Codex/                          ← Codex subsystem
├── Economy/                        ← Economy subsystem
├── Harvesting/                     ← Harvesting subsystem
├── Industries/                     ← Industries subsystem
├── Organizations/                  ← Organizations subsystem
├── Regions/                        ← Regions subsystem
├── Traits/                         ← Traits subsystem
└── ResourceNodes/                  ← Node provisioning
```

---

## Documentation

### Essential Reading
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Complete architecture guide
- **[IMPLEMENTATION_COMPLETE.md](IMPLEMENTATION_COMPLETE.md)** - Final summary
- **[REFACTORING_INDEX.md](REFACTORING_INDEX.md)** - Evolution timeline

### Phase Documentation
- [Phase 1: Strong Types](PHASE1_STRONG_TYPES.md)
- [Phase 2: Persona Abstraction](PHASE2_PERSONA_ABSTRACTION.md)
- [Phase 3: CQRS Migration](PHASE3_4_COMPLETE.md)
- [Phase 4: Event Bus](PHASE4_EVENT_BUS.md)
- [Phase 5: Public API](PHASE5_PUBLIC_API.md) (cancelled)

### Subsystem Documentation
- [Industries Complete](PHASE3_4_INDUSTRIES_COMPLETE.md)
- [Organizations Complete](PHASE3_4_ORGANIZATIONS_CQRS_COMPLETE.md)
- [Harvesting Complete](HARVESTING_SESSION_COMPLETE.md)
- [Regions Complete](Regions/REGIONS_CQRS_COMPLETE.md)
- [Traits Complete](Traits/TRAITS_CQRS_COMPLETE.md)

---

## Key Achievements

✅ **Type Safety** - Strong types replace primitives
✅ **Clean Architecture** - CQRS with clear boundaries
✅ **Event-Driven** - Loose coupling via domain events
✅ **Well-Tested** - 144 BDD tests, 100% passing
✅ **Pure Domain Logic** - No NWN dependencies in core
✅ **Production Ready** - All phases complete

---

## Contact

For questions about WorldEngine architecture or implementation, contact the development team.

---

**🎉 WorldEngine is production-ready and battle-tested!**

