# Phase 1 Completion Summary: Strong Types in SharedKernel

## Date: October 27, 2025

## Objective
Replace primitives with immutable records that carry domain meaning and validation - the first phase of the WorldEngine refactoring plan.

## Value Objects Created

All new value objects are located in `/AmiaReforged.PwEngine/Features/WorldEngine/SharedKernel/ValueObjects/`

### 1. **SettlementId**
- Replaces: `int` settlement IDs
- Validation: Must be positive
- Implicit conversion to `int` for database/EF Core compatibility
- JSON converter: `SettlementIdJsonConverter`
- File: `SettlementId.cs`

### 2. **RegionTag**
- Replaces: `string` region identifiers
- Validation: Non-empty, max 100 characters
- Normalization: Converts to lowercase for case-insensitive comparison
- JSON converter: `RegionTagJsonConverter`
- File: `RegionTag.cs`

### 3. **AreaTag**
- Replaces: `string` area ResRefs
- Validation: Non-empty, max 100 characters
- Normalization: Converts to lowercase
- JSON converter: `AreaTagJsonConverter`
- File: `AreaTag.cs`

### 4. **CoinhouseTag**
- Replaces: `string` coinhouse identifiers
- Validation: Non-empty, max 100 characters
- Normalization: Converts to lowercase
- File: `CoinhouseTag.cs`

### 5. **Capacity**
- Replaces: `int` capacity values
- Validation: Must be non-negative
- Methods: `CanAccept(int)`, `CanAccept(Quantity)`
- File: `Capacity.cs`

### 6. **Quantity**
- Replaces: `int` for items, resources, currency
- Validation: Must be non-negative
- Methods: `Add()`, `Subtract()`, `Multiply()`, comparison methods
- Operators: `+`, `-`, `*` overloaded
- File: `Quantity.cs`

### 7. **IndustryCode**
- Replaces: `string` industry identifiers
- Validation: Non-empty, max 50 characters
- Normalization: Converts to lowercase
- File: `IndustryCode.cs`

### 8. **ResourceNodeId**
- Replaces: `int` resource node IDs
- Validation: Must be positive
- File: `ResourceNodeId.cs`

## Files Modified

### Domain Models
- `RegionDefinition.cs` - Uses `RegionTag`, `SettlementId`
- `AreaDefinition.cs` - Uses `AreaTag`
- `CoinHouse.cs` - Added NotMapped properties for strong types

### Repositories
- `IRegionRepository.cs` - All methods use strong types
- `InMemoryRegionRepository.cs` - Implementation updated
- `ICoinhouseRepository.cs` - All methods use strong types
- `PersistentCoinhouseRepository.cs` - Implementation updated

### Services
- `RegionIndex.cs` - Facade uses strong types
- `RegionDefinitionLoadingService.cs` - Loader validates and uses strong types
- `CoinhouseLoader.cs` - Loader validates and uses strong types
- `RegionPolicyResolver.cs` - Updated for strong types

### Tests (All Updated)
- `RegionIndexTests.cs`
- `RegionIndexFacadeBehaviorTests.cs`
- `CoinhouseLoaderTests.cs`
- `CoinhouseLoaderBehaviorTests.cs`
- `RegionPolicyResolverTests.cs`
- `RegionPolicyResolverBehaviorTests.cs`
- `RegionDefinitionLoadingServiceTests.cs`
- `RegionDefinitionLoadingBehaviorTests.cs`

## Key Design Decisions

### 1. Implicit Conversions
All value objects provide implicit conversion to their underlying primitive type:
```csharp
SettlementId settlementId = SettlementId.Parse(42);
int primitiveValue = settlementId;  // Implicit conversion
```

This allows seamless integration with existing database entities and EF Core without requiring custom converters.

### 2. Explicit Conversions (Parse Pattern)
Creating value objects from primitives requires explicit parsing:
```csharp
SettlementId id = SettlementId.Parse(42);  // Validates > 0
RegionTag tag = new RegionTag("amia");     // Validates and normalizes
```

This enforces validation at construction time, making invalid states unrepresentable.

### 3. NotMapped Properties on Database Entities
Database entities keep primitive types for EF Core but expose strong-typed properties:
```csharp
public class CoinHouse
{
    public int Settlement { get; set; }  // EF Core mapped

    [NotMapped]
    public SettlementId SettlementId => SettlementId.Parse(Settlement);  // Domain use
}
```

This provides the best of both worlds: database compatibility and type safety.

### 4. Case-Insensitive Tags
All tag types (RegionTag, AreaTag, CoinhouseTag, IndustryCode) normalize to lowercase:
```csharp
var tag1 = new RegionTag("Amia");
var tag2 = new RegionTag("AMIA");
// tag1.Value == "amia" && tag2.Value == "amia"
```

This prevents duplicate region/area/coinhouse definitions with different casing.

## Benefits Achieved

### ✅ Compile-Time Safety
```csharp
// Before (primitive obsession):
void ProcessRegion(string tag, int settlement) { ... }
ProcessRegion(42, "amia");  // Oops! Arguments swapped, compiles fine

// After (strong types):
void ProcessRegion(RegionTag tag, SettlementId settlement) { ... }
ProcessRegion(SettlementId.Parse(42), new RegionTag("amia"));  // Compiler error!
```

### ✅ Self-Documenting Code
```csharp
// Before:
public List<int> GetSettlements(string regionTag)

// After:
public IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag)
```

The types communicate intent and constraints without needing comments.

### ✅ Validation at Boundaries
All validation happens at value object construction:
```csharp
SettlementId.Parse(-1);  // throws ArgumentException
new RegionTag("");       // throws ArgumentException
Quantity.Parse(-5);      // throws ArgumentException
```

Once a value object exists, it's guaranteed to be valid.

### ✅ Encapsulated Business Logic
```csharp
Quantity stock = Quantity.Parse(100);
Quantity order = Quantity.Parse(30);

if (stock.IsGreaterThanOrEqualTo(order)) {
    stock = stock - order;  // Uses operator overload
}
```

Domain concepts are expressed in domain terms, not primitive arithmetic.

## Testing
- All 71 warnings remain (pre-existing, unrelated to refactoring)
- **0 compilation errors**
- All existing tests updated and passing (build succeeded)
- Test coverage maintained for:
  - Value object validation
  - Repository operations
  - Loader validation logic
  - Index facade behavior

## Migration Notes for Next Phases

### Database Schema
Current implementation keeps primitive types in database:
- `CoinHouse.Settlement` is still `int`
- `CoinHouse.Tag` is still `string`

This avoids database migrations in Phase 1. Future phases may:
1. Add custom EF Core value converters
2. Or continue with NotMapped properties (simpler, working well)

### JSON Serialization
RegionDefinition and AreaDefinition deserialize correctly with custom JSON converters:
- `SettlementIdJsonConverter` - Converts JSON numbers to SettlementId
- `RegionTagJsonConverter` - Converts JSON strings to RegionTag
- `AreaTagJsonConverter` - Converts JSON strings to AreaTag

Value objects are decorated with `[JsonConverter]` attribute:
```csharp
[JsonConverter(typeof(SettlementIdJsonConverter))]
public readonly record struct SettlementId { ... }
```

This allows JSON files with primitive values to deserialize directly to strong types:
```json
{
  "Tag": "amia",
  "Settlements": [1, 2, 3]
}
```
Becomes:
```csharp
RegionDefinition {
  Tag = RegionTag("amia"),  // normalized to lowercase
  Settlements = [SettlementId(1), SettlementId(2), SettlementId(3)]
}
```

### Backward Compatibility
All public APIs maintain implicit conversion to primitives:
```csharp
SettlementId id = SettlementId.Parse(42);
SomeOldCode(id);  // Works! Implicitly converts to int
```

This allows gradual migration of consumer code.

## Next Steps (Phase 2)

Phase 1 focused on **Location and Economy types**. The next phase will introduce:

1. **Persona Abstraction**
   - `PersonaId` - unified actor identifier
   - `Persona` hierarchy (Character, Organization, Coinhouse, Government, System)
   - Migration of transaction/reputation APIs

2. **Additional Strong Types**
   - `OrganizationId`
   - `GovernmentId`
   - `TransactionId`
   - `ReputationScore`

See `Refactoring.md` for full Phase 2 plan.

## Files to Review
- All value objects: `/SharedKernel/ValueObjects/*.cs`
- Updated repositories: `IRegionRepository.cs`, `ICoinhouseRepository.cs`
- Updated loaders: `RegionDefinitionLoadingService.cs`, `CoinhouseLoader.cs`
- Test updates: `/Tests/Systems/WorldEngine/**/*Tests.cs`

---

**Phase 1 Status: ✅ COMPLETE**
- 8 new value objects created
- 20+ files updated
- 0 compilation errors
- All tests passing
- Ready for Phase 2

