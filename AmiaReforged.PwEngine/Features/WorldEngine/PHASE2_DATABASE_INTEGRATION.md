# Phase 2 Continuation: Database Integration

## Date: October 27, 2025

## Status: ✅ DATABASE ENTITIES UPDATED

The PersonaId abstraction has been integrated into the database layer.

## What Was Accomplished

### Database Entities Updated (3 entities)

#### 1. PersistedCharacter
**File:** `Database/Entities/PersistedCharacter.cs`

**Added:**
- `PersonaIdString` column (nullable string) - stores "Character:{Id}"
- `PersonaId` NotMapped property - returns strongly-typed PersonaId
  - Auto-generates from CharacterId if PersonaIdString is null
  - Parses from PersonaIdString if populated

**Usage:**
```csharp
var character = GetCharacter(id);
PersonaId personaId = character.PersonaId;  // Auto-generates or parses
```

#### 2. Organization
**File:** `Database/Entities/Organization.cs`

**Added:**
- Complete entity structure (was empty placeholder)
- `Id`, `Name`, `Description` properties
- `PersonaIdString` column (nullable string) - stores "Organization:{Id}"
- `OrganizationId` NotMapped property - returns strongly-typed OrganizationId
- `PersonaId` NotMapped property - returns strongly-typed PersonaId

**Usage:**
```csharp
var org = GetOrganization(id);
PersonaId personaId = org.PersonaId;  // "Organization:{guid}"
```

#### 3. CoinHouse
**File:** `Database/Entities/Economy/Treasuries/CoinHouse.cs`

**Added:**
- `PersonaIdString` column (nullable string) - stores "Coinhouse:{Tag}"
- `PersonaId` NotMapped property - returns strongly-typed PersonaId
  - Auto-generates from CoinhouseTag if PersonaIdString is null
  - Parses from PersonaIdString if populated

**Existing strong-typed properties maintained:**
- `CoinhouseTag`
- `SettlementId`
- `Balance`

**Usage:**
```csharp
var coinhouse = GetCoinhouse(tag);
PersonaId personaId = coinhouse.PersonaId;  // "Coinhouse:cordor-bank"
```

## Design Pattern

### Dual-Column Approach
Each entity has:
1. **String column** (`PersonaIdString`) - for EF Core persistence
2. **NotMapped property** (`PersonaId`) - for domain logic

This provides:
- ✅ Database compatibility (string storage)
- ✅ Type safety in code (PersonaId value object)
- ✅ Backward compatibility (nullable, auto-generates if null)
- ✅ Migration-friendly (existing data works without PersonaIdString)

### Example Pattern
```csharp
public class PersistedCharacter
{
    // EF Core maps this to database
    public string? PersonaIdString { get; set; }

    // Domain code uses this
    [NotMapped]
    public PersonaId PersonaId =>
        PersonaIdString != null
            ? PersonaId.Parse(PersonaIdString)
            : CharacterId.ToPersonaId();  // Fallback
}
```

## Compilation Status
✅ **0 errors**
⚠️ **Warnings:** "Possible performance issues" and "never used" (expected - setters will be used by EF Core)

## Next Steps

### Immediate: Database Migration

1. **Create EF Migration**
   ```bash
   cd AmiaReforged.PwEngine
   dotnet ef migrations add AddPersonaIdColumns --context PwEngineContext
   ```

2. **Add Data Migration SQL** to the generated migration file:
   ```sql
   UPDATE "PersistedCharacters"
   SET "PersonaIdString" = 'Character:' || "Id"::text
   WHERE "PersonaIdString" IS NULL;

   UPDATE "Organizations"
   SET "PersonaIdString" = 'Organization:' || "Id"::text
   WHERE "PersonaIdString" IS NULL;

   UPDATE "CoinHouses"
   SET "PersonaIdString" = 'Coinhouse:' || "Tag"
   WHERE "PersonaIdString" IS NULL;
   ```

3. **Apply Migration**
   ```bash
   dotnet ef database update --context PwEngineContext
   ```

4. **Verify Data**
   ```sql
   SELECT "Id", "PersonaIdString" FROM "PersistedCharacters" LIMIT 5;
   ```

### Next Phase: API Migration

Once database is migrated, update APIs:

1. **Transaction APIs** - Accept `PersonaId` instead of `CharacterId`
2. **Reputation APIs** - Accept `PersonaId` for any actor type
3. **Ownership APIs** - Use `PersonaId` for ownership tracking
4. **Industry APIs** - Use `PersonaId` for memberships

### Testing Strategy

1. **Database Tests**
   - Test PersonaId generation for characters
   - Test PersonaId generation for organizations
   - Test PersonaId generation for coinhouses
   - Test parsing from database values

2. **Integration Tests**
   - Test character transactions using PersonaId
   - Test organization transactions using PersonaId
   - Test cross-persona interactions

## Migration Guide

See: `Database/MIGRATION_GUIDE_PERSONAID.md` for detailed instructions

## Benefits Achieved

### ✅ Storage Ready
Database can now store PersonaId for:
- Characters
- Organizations
- Coinhouses

### ✅ Type Safety
Domain code uses strong types:
```csharp
PersonaId personaId = character.PersonaId;  // Type-safe, validated
```

### ✅ Backward Compatible
Existing data works without PersonaIdString:
```csharp
// If PersonaIdString is null, auto-generates from CharacterId
var personaId = character.PersonaId;  // Still works!
```

### ✅ Migration-Friendly
Can migrate data in stages:
1. Add columns (nullable)
2. Populate existing records
3. Start using for new records
4. Eventually make required (optional)

## Files Modified
```
Database/Entities/
  ├── PersistedCharacter.cs (updated - added PersonaIdString)
  ├── Organization.cs (updated - complete entity structure)
  └── Economy/Treasuries/
      └── CoinHouse.cs (updated - added PersonaIdString)

Database/
  └── MIGRATION_GUIDE_PERSONAID.md (new - migration instructions)
```

## Related Documentation
- `PHASE2_FOUNDATION_COMPLETE.md` - Phase 2 foundation summary
- `PERSONA_QUICK_REFERENCE.md` - Developer quick reference
- `Refactoring.md` - Overall refactoring plan

---

**Phase 2 Continuation Status: ✅ DATABASE LAYER COMPLETE**
- 3 entities updated
- PersonaId storage ready
- 0 compilation errors
- Ready for EF Core migration creation

**Next Milestone:** Create and apply EF Core migration, then begin API updates

