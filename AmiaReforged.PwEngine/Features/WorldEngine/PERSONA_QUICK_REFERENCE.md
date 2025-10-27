# Persona Quick Reference Guide

## When to Use What

### Use `CharacterId` when:
- Working with character-specific data (name, stats, inventory)
- Saving/loading character from database
- Character-only operations

### Use `OrganizationId` when:
- Working with organization-specific data (members, ranks)
- Organization-only operations

### Use `PersonaId` when:
- Transactions between any actors
- Reputation changes involving any actors
- Ownership/industry assignments
- Any cross-subsystem actor reference

## Quick Conversions

```csharp
// CharacterId → PersonaId
CharacterId charId = CharacterId.New();
PersonaId personaId = charId.ToPersonaId();

// OrganizationId → PersonaId
OrganizationId orgId = OrganizationId.New();
PersonaId personaId = orgId.ToPersonaId();

// GovernmentId → PersonaId
GovernmentId govId = GovernmentId.New();
PersonaId personaId = govId.ToPersonaId();

// CoinhouseTag → PersonaId
CoinhouseTag tag = new("cordor-bank");
PersonaId personaId = PersonaId.FromCoinhouse(tag);

// System Process → PersonaId
PersonaId personaId = PersonaId.FromSystem("TaxCollector");
```

## Creating Personas

```csharp
// Character
var character = CharacterPersona.Create(characterId, "Aldric");

// Organization
var org = OrganizationPersona.Create(orgId, "Merchants Guild");

// Coinhouse
var bank = CoinhousePersona.Create(
    new CoinhouseTag("cordor-bank"),
    SettlementId.Parse(1),
    "Cordor Central Bank"
);

// Government
var gov = GovernmentPersona.Create(
    govId,
    SettlementId.Parse(1),
    "City of Cordor"
);

// System Process
var system = SystemPersona.Create("TaxCollector");
// or with custom display name:
var system = SystemPersona.Create("TaxCollector", "Automated Tax Collection");
```

## Parsing PersonaId from String

```csharp
// From database or JSON
PersonaId personaId = PersonaId.Parse("Character:550e8400-e29b-41d4-a716-446655440000");

// Check type
if (personaId.Type == PersonaType.Character) {
    // Handle character
}

// Get underlying value
string guidString = personaId.Value;
```

## Pattern Matching

```csharp
Persona persona = GetPersonaSomehow();

switch (persona) {
    case CharacterPersona character:
        // Access character.CharacterId
        break;
    case OrganizationPersona org:
        // Access org.OrganizationId
        break;
    case CoinhousePersona coinhouse:
        // Access coinhouse.Tag
        break;
    case GovernmentPersona gov:
        // Access gov.GovernmentId
        break;
    case SystemPersona system:
        // Access system.ProcessName
        break;
}
```

## Test Helpers

```csharp
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;

// Quick test personas
var testChar = PersonaTestHelpers.CreateCharacterPersona("TestName");
var testOrg = PersonaTestHelpers.CreateOrganizationPersona("TestOrg");
var testBank = PersonaTestHelpers.CreateCoinhousePersona("test-bank", 5);
var testGov = PersonaTestHelpers.CreateGovernmentPersona(1, "Test Gov");
var testSystem = PersonaTestHelpers.CreateSystemPersona("TestProcess");
```

## Common Mistakes to Avoid

❌ **Don't** use PersonaId for domain-specific operations:
```csharp
// BAD - PersonaId doesn't know about character stats
var strength = GetCharacterStrength(personaId); // This won't work
```

✅ **Do** use strongly-typed IDs for domain operations:
```csharp
// GOOD - Use CharacterId for character-specific data
var strength = GetCharacterStrength(characterId);
```

❌ **Don't** create PersonaId manually for new entities:
```csharp
// BAD - Type might not match
var personaId = new PersonaId(PersonaType.Character, someId);
```

✅ **Do** use the factory methods:
```csharp
// GOOD - Type safety guaranteed
var personaId = PersonaId.FromCharacter(characterId);
// or
var persona = CharacterPersona.Create(characterId, name);
var personaId = persona.Id;
```

❌ **Don't** parse PersonaId without error handling:
```csharp
// BAD - Might throw if format is wrong
var personaId = PersonaId.Parse(userInput);
```

✅ **Do** handle parsing errors:
```csharp
// GOOD - Safe parsing
try {
    var personaId = PersonaId.Parse(databaseValue);
} catch (ArgumentException ex) {
    // Handle invalid format
}
```

## Migration Checklist

When updating an API to use Persona:

- [ ] Change parameter from `CharacterId` to `PersonaId`
- [ ] Update all call sites
- [ ] Add tests for non-character personas (org, coinhouse, etc.)
- [ ] Update database queries if needed
- [ ] Document the change in changelog

## Examples from Real Use Cases

### Before (Character-only):
```csharp
public void TransferGold(CharacterId from, CharacterId to, int amount) {
    // Only characters can transfer gold
}
```

### After (Any persona):
```csharp
public void TransferGold(PersonaId from, PersonaId to, Quantity amount) {
    // Characters, organizations, coinhouses, governments can transfer gold
}
```

### Usage:
```csharp
// Player pays guild dues
TransferGold(playerPersona.Id, guildPersona.Id, Quantity.Parse(100));

// Guild deposits to bank
TransferGold(guildPersona.Id, bankPersona.Id, Quantity.Parse(5000));

// Tax collection by system
TransferGold(merchantPersona.Id, taxCollectorPersona.Id, Quantity.Parse(50));
```

