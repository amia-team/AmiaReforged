# Property Eviction System Implementation

## Overview

Implemented a comprehensive property eviction system following DDD principles, inspired by the existing `PlayerStallRentRenewalService`. The system automatically evicts tenants from rentable properties when rent payments are overdue beyond the grace period.

## Architecture

### Components Created

1. **`PropertyEvictionService`** - Background service orchestrating eviction cycles
2. **`IPropertyEvictionExecutor`** - Interface for eviction execution
3. **`PropertyEvictionExecutor`** - Concrete implementation handling placeable deletion and state clearing

### Design Principles

- **Domain-Driven Design**: Clear separation between policy evaluation and execution
- **CQRS**: Uses existing query infrastructure (`IRentablePropertyRepository`)
- **Dependency Injection**: All services registered with `[ServiceBinding]`
- **Behavioral Testing**: Tests focus on WHAT the system accomplishes, not HOW

## Key Features

### Eviction Eligibility (Existing)

Uses the existing `PropertyRentalPolicy.IsEvictionEligible` method:
- Respects customizable grace periods (default: 2 days via `RentablePropertyDefinition.EvictionGraceDays`)
- Considers due dates (`RentalAgreementSnapshot.NextPaymentDueDate`)
- Tracks tenant activity (`LastOccupantSeenUtc`)
- Active tenants (seen after due date) are NOT evicted

### Eviction Phases

**Phase 1: Placeable Deletion**
- Queries all persistent placeables for the property's area
- Filters by character ID (extracted from `PersonaId`)
- Deletes from database via `IPersistentObjectRepository`
- Destroys in-game objects if they exist

**Phase 2: Property State Clearing**
- Clears all residents (`Residents = []`)
- Removes tenant assignment (`CurrentTenant = null`)
- Terminates rental agreement (`ActiveRental = null`)
- Sets status to vacant (`OccupancyStatus = Vacant`)

### Execution Cycle

1. **Startup Delay**: 5 minutes (allows server bootstrapping)
2. **Check Interval**: Every 1 hour
3. **Query**: `GetAllPropertiesAsync()` from repository
4. **Filter**: Only rented properties with active rental agreements
5. **Evaluate**: `PropertyRentalPolicy.IsEvictionEligible()`
6. **Execute**: Delete placeables → Clear state → Persist changes

## Implementation Details

### Property Eviction Service

```csharp
[ServiceBinding(typeof(PropertyEvictionService))]
public sealed class PropertyEvictionService : IDisposable
{
    private static readonly TimeSpan EvictionCheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    
    internal async Task ExecuteEvictionCycleAsync(CancellationToken token)
    {
        // Get all properties
        List<RentablePropertySnapshot> properties = await _repository.GetAllPropertiesAsync(token);
        
        // Filter rented properties with active rentals
        // Check eligibility via PropertyRentalPolicy
        // Execute eviction phases
        // Persist vacant state
    }
}
```

### Property Eviction Executor

```csharp
[ServiceBinding(typeof(IPropertyEvictionExecutor))]
public sealed class PropertyEvictionExecutor : IPropertyEvictionExecutor
{
    public async Task ExecuteEvictionAsync(RentablePropertySnapshot property, CancellationToken cancellationToken)
    {
        // Phase 1: Delete tenant placeables
        // - Resolve area ResRef from POI.Name (InternalName)
        // - Extract character ID from PersonaId
        // - Query and delete all character placeables in that area
    }
}
```

### Area Resolution

Uses `RegionIndex` to resolve property area from POI system:
```csharp
// POI.Name matches property InternalName by convention
if (!_regionIndex.TryGetSettlementForPointOfInterest(internalName, out SettlementId settlementId))
    return null;

IReadOnlyList<PlaceOfInterest> pois = _regionIndex.GetPointsOfInterestForSettlement(settlementId);
PlaceOfInterest? poi = pois.FirstOrDefault(p => p.Name == internalName);

// POI.ResRef is the area's ResRef
return poi?.ResRef;
```

### Character ID Extraction

```csharp
private async Task<Guid?> ResolveCharacterIdAsync(PersonaId? personaId)
{
    if (personaId?.Type == PersonaType.Character)
    {
        // PersonaId.Value is the string representation of the GUID
        if (Guid.TryParse(personaId.Value.Value, out Guid characterId))
            return characterId;
    }
    
    return null; // Only character personas supported for now
}
```

## Repository Changes

Extended `IRentablePropertyRepository` with new method:

```csharp
Task<List<RentablePropertySnapshot>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);
```

Implemented in `PersistentRentablePropertyRepository`:

```csharp
public async Task<List<RentablePropertySnapshot>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
{
    await using PwEngineContext ctx = factory.CreateDbContext();
    
    List<RentablePropertyRecord> entities = await ctx.RentableProperties
        .Include(p => p.Residents)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
        
    return entities.Select(ToSnapshot).ToList();
}
```

## Testing Approach

Created behavioral tests emphasizing **outcomes** over **implementation**:

### Example Test: Grace Period Behavior

```csharp
[Fact]
public async Task WhenRentalIsPastDueWithinGracePeriod_ShouldNotEvict()
{
    // Arrange
    DateTimeOffset now = new(2025, 11, 6, 12, 0, 0, TimeSpan.Zero);
    DateTimeOffset dueDate = now.AddDays(-1); // 1 day overdue, grace is 2 days
    
    RentablePropertySnapshot property = CreateRentedProperty(dueDate, TestTenant);
    
    // Act
    await service.ExecuteEvictionCycleAsync(CancellationToken.None);
    
    // Assert - Behavior: Property should remain rented during grace period
    executor.EvictedProperties.Should().BeEmpty("grace period has not expired");
    property.OccupancyStatus.Should().Be(PropertyOccupancyStatus.Rented);
}
```

### Test Coverage

- ✅ Grace period enforcement (within/expired)
- ✅ Tenant activity tracking (prevents eviction if active)
- ✅ Vacant property handling (ignored)
- ✅ Owned property handling (never evicted)
- ✅ Resident clearing on eviction
- ✅ Status transition to Vacant
- ✅ Tenant and rental agreement clearing
- ✅ Placeable deletion execution
- ✅ Custom grace day override support
- ✅ Multiple property processing

## Differences from Player Stall Service

### Similarities
- Background service with periodic timer
- Grace period implementation
- State clearing on suspension
- Notification capability (stubbed out for properties)

### Differences
- **No payment processing**: Stalls attempt rent withdrawal; properties just evict
- **No escrow/refunds**: Stalls handle prorated refunds; properties don't
- **No inventory transfer**: Stalls move items to reeve; properties delete placeables
- **Simpler lifecycle**: Properties go straight from Rented → Vacant

## Future Enhancements

1. **Notifications**: Add in-game notifications or email to tenants before eviction
2. **Placeable Archiving**: Instead of deletion, move placeables to a "reeve storage" system
3. **Payment Attempts**: Add automatic payment retry before eviction
4. **Eviction Logs**: Track eviction history for analytics
5. **Player Persona Support**: Extend character ID resolution to handle player personas
6. **Grace Period Warnings**: Send warnings when entering grace period

## Configuration

Eviction timing can be adjusted in `PropertyEvictionService`:

```csharp
private static readonly TimeSpan EvictionCheckInterval = TimeSpan.FromHours(1);
private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
```

Grace periods are configured per-property in `RentablePropertyDefinition`:

```csharp
public sealed record RentablePropertyDefinition(
    // ... other parameters
    int EvictionGraceDays = 2)  // Default: 2 days
```

## Logging

Service logs eviction actions at appropriate levels:

- `Debug`: Evaluation cycle start, property counts
- `Info`: Successful evictions, placeable deletion counts
- `Warn`: Missing POI mappings, character ID resolution failures
- `Error`: Eviction processing failures

## Integration Points

- **RegionIndex**: POI → Area ResRef resolution
- **IRentablePropertyRepository**: Property state queries and persistence
- **IPersistentObjectRepository**: Placeable deletion
- **PropertyRentalPolicy**: Eviction eligibility evaluation
- **RuntimeCharacterService**: Character ID tracking (not used in final implementation)

## Build Status

✅ Successfully compiles with no errors (109 warnings are pre-existing)

```bash
dotnet build AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj
# 109 Warning(s)
# 0 Error(s)
```

## Files Created

1. `/Features/WorldEngine/Economy/Properties/PropertyEvictionService.cs` (216 lines)
2. `/Features/WorldEngine/Economy/Properties/IPropertyEvictionExecutor.cs` (15 lines)
3. `/Features/WorldEngine/Economy/Properties/PropertyEvictionExecutor.cs` (206 lines)

## Files Modified

1. `/Features/WorldEngine/Economy/Properties/IRentablePropertyRepository.cs` - Added `GetAllPropertiesAsync`
2. `/Database/PersistentRentablePropertyRepository.cs` - Implemented `GetAllPropertiesAsync`

## Next Steps

1. **In-game testing**: Verify eviction timing and placeable deletion
2. **Monitoring**: Watch logs for eviction cycles in production
3. **Metrics**: Track eviction rates and reasons
4. **Documentation**: Update user-facing property rental guides
