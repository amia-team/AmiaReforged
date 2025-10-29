# WorldSimulator Architecture Assessment

**Date**: October 29, 2025
**Status**: In Progress - Refactoring Needed

## Clear Architecture Vision

### WorldSimulator Responsibilities
- **AI & Actor Simulation**: Run complex AI behaviors, NPC actions, faction strategies
- **Heavy Calculations**: Economic models, dominion turns, civic stat aggregation
- **Command Generation**: Send actionable commands to WorldEngine based on simulation results
- **Autonomous Processes**: Time-based events, background processes that don't need real-time player interaction

### WorldEngine Responsibilities
- **Business/Game Rules**: All D&D/NWN game mechanics and validation
- **Domain ↔ Game Interop**: Character facades, item facades, area management
- **Player-Facing Features**: Real-time interactions, UI, spellcasting, combat
- **Authoritative State**: Single source of truth for game world state

### Communication Pattern
```
WorldSimulator: AI calculates "Baron should raise taxes"
    ↓ (Command)
WorldEngine: Validates domain rules, applies to game state
    ↓ (Event/Response)
WorldSimulator: Updates local simulation state
```

---

## Current State Inventory

### ✅ What's Good

#### 1. **Project Structure**
- Proper layered architecture (Domain, Application, Infrastructure)
- Clean separation from game server (no circular dependencies)
- Independent database instance
- Circuit breaker pattern for resilience

#### 2. **Domain Events & Aggregates**
```csharp
// Good: Strongly typed events
public record SimulationServiceStarted(string Environment) : ISimulationEvent;
public record WorkItemCompleted(Guid WorkItemId, string WorkType, TimeSpan Duration) : ISimulationEvent;
public record CircuitBreakerStateChanged(string NewState, string WorldEngineHost, string? Error) : ISimulationEvent;
```

#### 3. **Value Objects**
```csharp
// Good: Type-safe enums
public enum WorkItemStatus { Pending, Processing, Completed, Failed }
public enum CircuitState { Closed, Open, HalfOpen }
public enum EventSeverity { Information, Warning, Error, Critical }
```

#### 4. **Documentation**
- Comprehensive requirements
- Architecture diagrams
- Communication patterns documented
- Setup instructions

---

## ❌ Major Issues - Primitive Obsession

### Issue #1: String-Based Work Types

**Current (BAD)**:
```csharp
// WorldSimulator/Domain/Aggregates/SimulationWorkItem.cs
public class SimulationWorkItem
{
    public string WorkType { get; private set; } = null!;  // ❌ Stringly-typed!
    public string Payload { get; private set; } = null!;    // ❌ Untyped JSON blob!
}

// SimulationWorker.cs
switch (workItem.WorkType)  // ❌ Runtime string matching
{
    case "DominionTurn":    // ❌ Magic strings everywhere!
    case "CivicStats":
    case "PersonaAction":
    case "MarketPricing":
}
```

**Should Be (GOOD)**:
```csharp
// Define work type as discriminated union
public abstract record WorkType
{
    private WorkType() { }

    public sealed record DominionTurn(GovernmentId GovernmentId, TurnDate TurnDate) : WorkType;
    public sealed record CivicStats(SettlementId SettlementId, DateTimeOffset Since) : WorkType;
    public sealed record PersonaAction(PersonaId PersonaId, ActionType Action) : WorkType;
    public sealed record MarketPricing(MarketId MarketId, ItemId ItemId) : WorkType;
}

// SimulationWorkItem.cs
public class SimulationWorkItem
{
    public WorkType WorkType { get; private set; } = null!;  // ✅ Compile-time safe!
}

// SimulationWorker.cs
var result = workItem.WorkType switch  // ✅ Pattern matching with exhaustiveness checking
{
    WorkType.DominionTurn dt => await ProcessDominionTurnAsync(dt, ct),
    WorkType.CivicStats cs => await ProcessCivicStatsAsync(cs, ct),
    WorkType.PersonaAction pa => await ProcessPersonaActionAsync(pa, ct),
    WorkType.MarketPricing mp => await ProcessMarketPricingAsync(mp, ct),
    _ => throw new UnreachableException()  // Compiler ensures all cases covered
};
```

### Issue #2: Untyped Payload Strings

**Current (BAD)**:
```csharp
private Task ProcessDominionTurnAsync(string payload, SimulationDbContext db, CancellationToken ct)
{
    // ❌ Have to deserialize blindly, no compile-time safety
    var data = JsonSerializer.Deserialize<DominionTurnPayload>(payload);
}
```

**Should Be (GOOD)**:
```csharp
private Task ProcessDominionTurnAsync(WorkType.DominionTurn command, CancellationToken ct)
{
    // ✅ Already have the data, compile-time safe!
    var governmentId = command.GovernmentId;
    var turnDate = command.TurnDate;
}
```

### Issue #3: Weak Value Objects for IDs

**Current (BAD)**:
```csharp
// Probably using strings or Guids directly
public void ProcessTurn(string governmentId)  // ❌ Could pass any string!
```

**Should Be (GOOD)**:
```csharp
public readonly record struct GovernmentId(Guid Value)
{
    public static GovernmentId Parse(string input)
    {
        if (!Guid.TryParse(input, out var guid))
            throw new FormatException($"Invalid GovernmentId: {input}");
        return new GovernmentId(guid);
    }

    public static GovernmentId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public void ProcessTurn(GovernmentId governmentId)  // ✅ Can't pass wrong ID type!
```

### Issue #4: DominionTurnJob Uses String for Government

**Current (BAD)**:
```csharp
public class DominionTurnJob
{
    public string GovernmentName { get; private set; } = null!;  // ❌ Not an ID, just a name?
}
```

**Should Be (GOOD)**:
```csharp
public class DominionTurnJob
{
    public GovernmentId GovernmentId { get; private set; }
    public string GovernmentName { get; private set; } = null!;  // Keep for display, but ID is primary
}
```

---

## 🎯 Refactoring Plan - Parse, Don't Validate!

### Phase 1: Strongly-Typed IDs (High Priority)

Create value objects for all domain identifiers:

```csharp
// WorldSimulator/Domain/ValueObjects/Identifiers.cs
namespace WorldSimulator.Domain.ValueObjects;

public readonly record struct GovernmentId(Guid Value)
{
    public static GovernmentId Parse(string input) =>
        Guid.TryParse(input, out var guid)
            ? new GovernmentId(guid)
            : throw new FormatException($"Invalid GovernmentId: {input}");

    public static GovernmentId New() => new(Guid.NewGuid());
}

public readonly record struct SettlementId(Guid Value)
{
    public static SettlementId Parse(string input) =>
        Guid.TryParse(input, out var guid)
            ? new SettlementId(guid)
            : throw new FormatException($"Invalid SettlementId: {input}");

    public static SettlementId New() => new(Guid.NewGuid());
}

public readonly record struct PersonaId(Guid Value)
{
    public static PersonaId Parse(string input) =>
        Guid.TryParse(input, out var guid)
            ? new PersonaId(guid)
            : throw new FormatException($"Invalid PersonaId: {input}");

    public static PersonaId New() => new(Guid.NewGuid());
}

public readonly record struct MarketId(Guid Value)
{
    public static MarketId Parse(string input) =>
        Guid.TryParse(input, out var guid)
            ? new MarketId(guid)
            : throw new FormatException($"Invalid MarketId: {input}");

    public static MarketId New() => new(Guid.NewGuid());
}

public readonly record struct ItemId(string ResRef)
{
    public static ItemId Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 16)
            throw new FormatException($"Invalid ItemId: {input}");
        return new ItemId(input.ToLowerInvariant());
    }
}
```

**Benefits**:
- ✅ Can't accidentally pass SettlementId where GovernmentId expected
- ✅ Parse method validates at construction time
- ✅ Explicit conversion required (no silent bugs)
- ✅ Serializes naturally (record struct)

### Phase 2: Discriminated Union for WorkType (High Priority)

```csharp
// WorldSimulator/Domain/ValueObjects/WorkType.cs
namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Discriminated union representing all work types the simulator can process.
/// Uses sealed records for compile-time exhaustiveness checking.
/// </summary>
public abstract record WorkType
{
    private WorkType() { }  // Prevent external inheritance

    public sealed record DominionTurn(
        GovernmentId GovernmentId,
        TurnDate TurnDate) : WorkType;

    public sealed record CivicStatsAggregation(
        SettlementId SettlementId,
        DateTimeOffset SinceTimestamp) : WorkType;

    public sealed record PersonaAction(
        PersonaId PersonaId,
        PersonaActionType ActionType,
        InfluenceAmount Cost) : WorkType;

    public sealed record MarketPricing(
        MarketId MarketId,
        ItemId ItemId,
        DemandSignal DemandSignal) : WorkType;
}
```

**Benefits**:
- ✅ Compiler enforces exhaustiveness in switch expressions
- ✅ Each work type has its own strongly-typed payload
- ✅ No JSON serialization needed for internal processing
- ✅ Refactoring-safe (rename detection, find usages)

### Phase 3: Additional Value Objects (Medium Priority)

```csharp
// TurnDate - not just a DateTime!
public readonly record struct TurnDate
{
    private readonly DateOnly _date;

    public TurnDate(DateOnly date)
    {
        // Validate: turns only happen on specific days
        if (date.Day != 1 && date.Day != 15)
            throw new ArgumentException("Turn dates must be 1st or 15th of month");
        _date = date;
    }

    public static TurnDate Parse(string input) => new(DateOnly.Parse(input));
    public TurnDate Next() => new(_date.AddDays(14));  // Always 2 weeks
}

// InfluenceAmount - not just an int!
public readonly record struct InfluenceAmount
{
    public int Value { get; }

    public InfluenceAmount(int value)
    {
        if (value < 0)
            throw new ArgumentException("Influence cannot be negative", nameof(value));
        Value = value;
    }

    public static InfluenceAmount Zero => new(0);
    public static InfluenceAmount operator +(InfluenceAmount a, InfluenceAmount b) =>
        new(a.Value + b.Value);
    public static InfluenceAmount operator -(InfluenceAmount a, InfluenceAmount b) =>
        new(Math.Max(0, a.Value - b.Value));
}

// DemandSignal - not just a float!
public readonly record struct DemandSignal
{
    public decimal Multiplier { get; }

    public DemandSignal(decimal multiplier)
    {
        if (multiplier < 0.1m || multiplier > 10.0m)
            throw new ArgumentException("Demand multiplier must be between 0.1 and 10.0");
        Multiplier = multiplier;
    }

    public static DemandSignal Normal => new(1.0m);
    public static DemandSignal Parse(decimal value) => new(value);
}
```

### Phase 4: Refactor SimulationWorkItem (High Priority)

**Current**:
```csharp
public class SimulationWorkItem
{
    public string WorkType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
}
```

**Refactored**:
```csharp
public class SimulationWorkItem
{
    private string _serializedWorkType = null!;  // For EF Core persistence

    public WorkType WorkType { get; private set; } = null!;
    public WorkItemStatus Status { get; private set; }

    private SimulationWorkItem() { }  // EF Core

    public static SimulationWorkItem Create(WorkType workType)
    {
        return new SimulationWorkItem
        {
            Id = Guid.NewGuid(),
            WorkType = workType,
            _serializedWorkType = JsonSerializer.Serialize(workType),
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    // EF Core navigation - serialize/deserialize for persistence
    public void OnAfterLoad()
    {
        WorkType = JsonSerializer.Deserialize<WorkType>(_serializedWorkType)!;
    }
}
```

### Phase 5: Refactor SimulationWorker (High Priority)

**Current (BAD)**:
```csharp
switch (workItem.WorkType)
{
    case "DominionTurn":
        await ProcessDominionTurnAsync(workItem.Payload, db, cancellationToken);
        break;
    // ...
}

private Task ProcessDominionTurnAsync(string payload, SimulationDbContext db, CancellationToken ct)
{
    var data = JsonSerializer.Deserialize<DominionTurnPayload>(payload);
    // ...
}
```

**Refactored (GOOD)**:
```csharp
var result = workItem.WorkType switch
{
    WorkType.DominionTurn dt => await ProcessDominionTurnAsync(dt, cancellationToken),
    WorkType.CivicStatsAggregation cs => await ProcessCivicStatsAsync(cs, cancellationToken),
    WorkType.PersonaAction pa => await ProcessPersonaActionAsync(pa, cancellationToken),
    WorkType.MarketPricing mp => await ProcessMarketPricingAsync(mp, cancellationToken),
    _ => throw new UnreachableException($"Unknown work type: {workItem.WorkType}")
};

// ✅ Strongly typed, no deserialization needed!
private async Task ProcessDominionTurnAsync(WorkType.DominionTurn command, CancellationToken ct)
{
    _logger.LogInformation(
        "Processing dominion turn for government {GovernmentId} on {TurnDate}",
        command.GovernmentId, command.TurnDate);

    // Request data from WorldEngine
    var territories = await _worldEngineClient.GetTerritoriesAsync(command.GovernmentId, ct);

    // Execute calculations
    var results = await _dominionTurnProcessor.ExecuteAsync(territories, command.TurnDate, ct);

    // Send results back to WorldEngine
    await _worldEngineClient.SubmitDominionTurnResultsAsync(results, ct);
}
```

---

## 🔧 Implementation Checklist

### Week 1: Foundation
- [ ] Create `Identifiers.cs` with all ID value objects (GovernmentId, SettlementId, etc.)
- [ ] Create `WorkType.cs` discriminated union
- [ ] Create domain value objects (TurnDate, InfluenceAmount, DemandSignal)
- [ ] Write unit tests for all value objects (parsing, validation, operators)

### Week 2: Refactor Core
- [ ] Refactor `SimulationWorkItem` to use `WorkType` instead of strings
- [ ] Update EF Core configuration for JSON serialization
- [ ] Refactor `SimulationWorker` to use pattern matching
- [ ] Remove all `string payload` parameters
- [ ] Update tests

### Week 3: Domain Model
- [ ] Refactor `DominionTurnJob` to use `GovernmentId`
- [ ] Create command objects for WorldEngine API calls
- [ ] Create response DTOs with proper value objects
- [ ] Update processors to use strongly-typed data

### Week 4: Integration
- [ ] Create `IWorldEngineClient` interface with typed methods
- [ ] Implement HTTP client with proper serialization
- [ ] Update all processors to use typed commands
- [ ] Integration tests with WireMock
- [ ] Update documentation

---

## 💡 What Would Kent Beck & Dave Farley Do?

### Kent Beck's Principles
1. **Make the change easy, then make the easy change**
   - First: Create value objects and discriminated unions
   - Then: Refactor existing code to use them

2. **Test-Driven Development**
   - Write tests for value objects FIRST
   - Red → Green → Refactor
   - Tests document expected behavior

3. **Simple Design**
   - No premature abstraction
   - Value objects are as simple as possible
   - Clear naming (GovernmentId, not EntityId<Government>)

### Dave Farley's Principles
1. **Parse, Don't Validate**
   - ✅ `GovernmentId.Parse(string)` - validation at construction
   - ✅ Once you have a `GovernmentId`, it's VALID
   - ❌ No need to re-validate everywhere

2. **Make Illegal States Unrepresentable**
   - ✅ Can't have negative InfluenceAmount
   - ✅ Can't have turn date on wrong day
   - ✅ Can't pass SettlementId where GovernmentId expected

3. **Continuous Delivery**
   - Refactor incrementally
   - Keep tests green at all times
   - Deploy small, safe changes frequently

---

## 📊 Before & After Comparison

### Before (Current)
```csharp
// ❌ Fragile, error-prone
var workItem = new SimulationWorkItem
{
    WorkType = "DominionTurn",  // Magic string
    Payload = JsonSerializer.Serialize(new
    {
        governmentId = "123e4567-e89b-12d3-a456-426614174000",  // String
        turnDate = "2025-10-15"  // String
    })
};

// Runtime errors possible
switch (workItem.WorkType)  // Might forget a case
{
    case "DominionTurn":
        var data = JsonSerializer.Deserialize<dynamic>(workItem.Payload);  // 😱
        ProcessTurn(data.governmentId.ToString());  // Multiple conversions
        break;
}
```

### After (Target)
```csharp
// ✅ Compile-time safe, refactor-friendly
var workItem = SimulationWorkItem.Create(
    new WorkType.DominionTurn(
        GovernmentId: GovernmentId.Parse("123e4567-e89b-12d3-a456-426614174000"),
        TurnDate: new TurnDate(new DateOnly(2025, 10, 15))
    )
);

// Compiler ensures exhaustiveness
var result = workItem.WorkType switch
{
    WorkType.DominionTurn dt => ProcessTurn(dt.GovernmentId, dt.TurnDate),
    WorkType.CivicStatsAggregation cs => ProcessCivicStats(cs.SettlementId, cs.SinceTimestamp),
    WorkType.PersonaAction pa => ProcessPersonaAction(pa.PersonaId, pa.ActionType, pa.Cost),
    WorkType.MarketPricing mp => ProcessMarketPricing(mp.MarketId, mp.ItemId, mp.DemandSignal),
    _ => throw new UnreachableException()
};
```

---

## 🎯 Success Metrics

- [ ] Zero magic strings in domain layer
- [ ] Zero `JsonSerializer.Deserialize` in business logic
- [ ] 100% exhaustiveness checking on WorkType switches
- [ ] All IDs are typed value objects
- [ ] All domain values have validation at construction
- [ ] Compiler catches type mismatches (not runtime)

---

## Next Steps

1. **Create value objects file** with all IDs and domain types
2. **Write comprehensive tests** for value objects (TDD)
3. **Refactor one workflow end-to-end** (e.g., DominionTurn)
4. **Validate with team** that pattern works
5. **Apply pattern to remaining workflows**
6. **Update documentation** with new patterns

The goal: **Make wrong code look wrong and refuse to compile!**

