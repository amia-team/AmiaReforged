# Trait System

A flexible, point-based character customization system that allows players to select background traits that define their character's strengths, weaknesses, and unique characteristics.

## Overview

The Trait System provides:
- **Point-based selection**: Characters receive 2 base trait points to spend
- **Positive & Negative traits**: Positive traits cost points, negative traits grant points
- **Eligibility rules**: Race/class restrictions and prerequisites
- **Death mechanics**: Different behaviors (persist, reset, permadeath)
- **Effect application**: Mechanical bonuses (skills, attributes, etc.)
- **DM unlocks**: Special traits that require DM permission
- **Persistence**: Traits are saved to the database

## Core Concepts

### Trait Budget

All characters start with 2 base trait points. Points can be:
- **Spent** on positive traits (costs points)
- **Gained** from negative traits (grants points)
- **Earned** through DM events

```csharp
TraitBudget budget = TraitBudget.CreateDefault();
Console.WriteLine($"Available points: {budget.AvailablePoints}"); // 2

// After selecting a positive trait (costs 1) and negative trait (grants 1)
budget = budget.AfterSpending(1 - 1); // Net cost: 0
Console.WriteLine($"Available points: {budget.AvailablePoints}"); // 2
```

### Trait Lifecycle States

Traits move through several states:

1. **Selectable** - Player can choose from eligible traits
2. **Selected** - Player has chosen trait (unconfirmed, can be changed)
3. **Confirmed** - Player finalizes selection (permanent, affects budget)
4. **Active** - Trait effects are applied to character

```csharp
CharacterTrait trait = new()
{
    TraitTag = "brave",
    IsConfirmed = false,  // Selected but not finalized
    IsActive = true       // Effects are active
};
```

### Death Behaviors

Traits can respond differently to character death:

```csharp
public enum TraitDeathBehavior
{
    Persist = 0,        // Nothing happens (default)
    ResetOnDeath = 1,   // Deactivate, clear CustomData (Hero trait)
    Permadeath = 2,     // Trigger permadeath (Villain trait)
    RemoveOnDeath = 3   // Delete trait entirely
}
```

## Defining Traits

### JSON Definition

Create trait definitions in `Resources/Traits/*.json`:

```json
{
  "Tag": "brave",
  "Name": "Brave",
  "Description": "You are fearless in the face of danger. Gain a bonus to Intimidate checks.",
  "PointCost": 1,
  "RequiresUnlock": false,
  "DeathBehavior": 0,
  "Effects": [
    {
      "EffectType": 1,
      "Target": "Intimidate",
      "Magnitude": 2,
      "Description": "+2 to Intimidate checks"
    }
  ],
  "AllowedRaces": [],
  "AllowedClasses": [],
  "ForbiddenRaces": [],
  "ForbiddenClasses": [],
  "ConflictingTraits": ["cowardly"],
  "PrerequisiteTraits": []
}
```

### Code Definition

```csharp
Trait brave = new()
{
    Tag = "brave",
    Name = "Brave",
    Description = "Fearless in combat",
    PointCost = 1,
    RequiresUnlock = false,
    DeathBehavior = TraitDeathBehavior.Persist,
    Effects =
    [
        TraitEffect.SkillModifier("Intimidate", 2)
    ],
    ConflictingTraits = ["cowardly"]
};
```

## Usage Examples

### 1. Validating Trait Selection

```csharp
// Character info
ICharacterInfo character = new TestCharacterInfo
{
    Race = new RaceData("Human"),
    Classes = [CharacterClassData.From("Fighter", 5)]
};

// Selected traits
List<Trait> selectedTraits = [];

// Trait to validate
Trait brave = traitRepository.Get("brave");

// Validate selection
TraitSelectionValidator validator = new(traitRepository);
bool canSelect = validator.CanSelectTrait(brave, character, selectedTraits);

if (!canSelect)
{
    Console.WriteLine("Cannot select this trait!");
}
```

### 2. Calculating Available Points

```csharp
ICharacterTraitRepository repo = new PersistentCharacterTraitRepository(dbContext);
Guid characterId = player.CharacterId;

// Get character's traits
List<CharacterTrait> traits = repo.GetByCharacterId(characterId);
List<CharacterTrait> confirmed = traits.Where(t => t.IsConfirmed).ToList();

// Calculate spent points
int spentPoints = confirmed.Sum(ct =>
{
    Trait? definition = traitRepository.Get(ct.TraitTag);
    return definition?.PointCost ?? 0;
});

// Create budget
TraitBudget budget = new()
{
    EarnedPoints = 0,
    SpentPoints = spentPoints
};

Console.WriteLine($"Available: {budget.AvailablePoints}"); // 2 - spentPoints
```

### 3. Selecting a Trait

```csharp
// Create new character trait (unconfirmed)
CharacterTrait newTrait = new()
{
    Id = Guid.NewGuid(),
    CharacterId = player.CharacterId,
    TraitTag = "brave",
    DateAcquired = DateTime.UtcNow,
    IsConfirmed = false,  // Not finalized yet
    IsActive = true,      // Effects active immediately
    IsUnlocked = false
};

repo.Add(newTrait);
```

### 4. Confirming Trait Selection

```csharp
// Player finalizes their choices
List<CharacterTrait> unconfirmed = repo
    .GetByCharacterId(characterId)
    .Where(t => !t.IsConfirmed)
    .ToList();

// Validate budget before confirming
int totalCost = unconfirmed.Sum(ct =>
{
    Trait? def = traitRepository.Get(ct.TraitTag);
    return def?.PointCost ?? 0;
});

TraitBudget budget = TraitBudget.CreateDefault().AfterSpending(totalCost);

if (budget.AvailablePoints >= 0)
{
    // Confirm all traits
    foreach (CharacterTrait ct in unconfirmed)
    {
        ct.IsConfirmed = true;
        repo.Update(ct);
    }
}
```

### 5. Applying Trait Effects

```csharp
TraitEffectApplicationService effectService = new(
    traitRepository,
    characterTraitRepository
);

// Get all active effects for character
List<(string TraitTag, TraitEffect Effect)> effects =
    effectService.GetActiveEffects(characterId);

// Apply effects to NWN creature
foreach ((string traitTag, TraitEffect effect) in effects)
{
    switch (effect.EffectType)
    {
        case TraitEffectType.SkillModifier:
            // Apply skill bonus with unique tag
            string tag = TraitEffectApplicationService.GenerateEffectTag(traitTag, 0);
            ApplySkillBonus(creature, effect.Target, effect.Magnitude, tag);
            break;

        case TraitEffectType.AttributeModifier:
            // Apply attribute bonus
            ApplyAttributeBonus(creature, effect.Target, effect.Magnitude);
            break;
    }
}
```

### 6. Handling Death

```csharp
TraitDeathHandler deathHandler = new(
    traitRepository,
    characterTraitRepository
);

// Character dies
bool shouldPermadeath = deathHandler.ProcessDeath(
    characterId,
    killedByHero: true
);

if (shouldPermadeath)
{
    // Trigger permadeath (Villain killed by Hero)
    DeleteCharacter(characterId);
}
else
{
    // Respawn character
    RespawnCharacter(characterId);
}
```

### 7. Hero Trait Rebuilding

```csharp
// Hero character dies and respawns
bool shouldPermadeath = deathHandler.ProcessDeath(characterId, killedByHero: false);

// After respawn, Hero can rebuild bonuses
// Traits with ResetOnDeath are deactivated and CustomData cleared
List<CharacterTrait> resetTraits = repo
    .GetByCharacterId(characterId)
    .Where(ct => !ct.IsActive && ct.IsConfirmed)
    .ToList();

if (resetTraits.Any())
{
    // Show rebuild UI
    ShowHeroRebuildUI(characterId, resetTraits);
}

// When player rebuilds, reactivate traits
deathHandler.ReactivateResettableTraits(characterId);
```

### 8. DM Unlocking Traits

```csharp
// DM grants special trait unlock
CharacterTrait unlock = new()
{
    Id = Guid.NewGuid(),
    CharacterId = player.CharacterId,
    TraitTag = "hero",
    DateAcquired = DateTime.UtcNow,
    IsConfirmed = false,  // Not selected yet
    IsActive = false,     // Not active yet
    IsUnlocked = true     // Unlocked by DM
};

repo.Add(unlock);

// Player can now select this trait in character creation
// (normally RequiresUnlock traits are hidden)
```

### 9. Loading Traits from JSON

```csharp
// Automatically called at startup
ITraitRepository traitRepo = InMemoryTraitRepository.Create();
TraitDefinitionLoadingService loader = new(traitRepo);

loader.Load(); // Reads from RESOURCE_PATH/Traits/*.json

// Check for failures
List<FileLoadResult> failures = loader.Failures();
if (failures.Any())
{
    foreach (FileLoadResult failure in failures)
    {
        Console.WriteLine($"Failed to load {failure.FileName}: {failure.Message}");
    }
}

// Use loaded traits
Trait? brave = traitRepo.Get("brave");
```

### 10. Strong Types for Race/Class

```csharp
// Use strong types instead of strings
RaceData human = new("Human");
CharacterClassData fighter = new("Fighter", 5, Array.Empty<SkillData>());

// Implicit conversion from string
RaceData elf = "Elf";
CharacterClassData wizard = "Wizard";

// Factory methods for convenience
CharacterClassData cleric = CharacterClassData.From("Cleric", 3);
CharacterClassData rogue = CharacterClassData.From("Rogue"); // Level 1 default

ICharacterInfo character = new TestCharacterInfo
{
    Race = human,
    Classes = [fighter, wizard]
};
```

## Effect Types

```csharp
public enum TraitEffectType
{
    None = 0,
    SkillModifier = 1,      // +/- to skill checks
    AttributeModifier = 2,  // +/- to attribute scores
    KnowledgePoints = 3,    // Grant knowledge points
    Custom = 99             // Custom scripted behavior
}
```

### Creating Effects

```csharp
// Skill modifier
TraitEffect skillBonus = TraitEffect.SkillModifier("Persuasion", 2);

// Attribute modifier
TraitEffect strBonus = TraitEffect.AttributeModifier("Strength", 1);

// Knowledge points
TraitEffect knowledge = TraitEffect.KnowledgePoints("Arcana", 5);

// Custom effect (implement in game code)
TraitEffect custom = new()
{
    EffectType = TraitEffectType.Custom,
    Description = "Special scripted behavior"
};
```

## Architecture

### Domain Model

```
Trait (Definition/Template)
  ├─ Tag (unique identifier)
  ├─ Name, Description
  ├─ PointCost
  ├─ DeathBehavior
  ├─ Effects[]
  ├─ Eligibility rules (race/class)
  └─ Relationships (conflicts, prerequisites)

CharacterTrait (Instance/Selection)
  ├─ CharacterId
  ├─ TraitTag (references Trait)
  ├─ IsConfirmed (finalized?)
  ├─ IsActive (effects applied?)
  ├─ IsUnlocked (DM granted?)
  └─ CustomData (state storage)
```

### Repositories

- **`ITraitRepository`**: In-memory trait definitions (loaded from JSON)
- **`ICharacterTraitRepository`**: Persistent character trait selections (database)

### Services

- **`TraitDefinitionLoadingService`**: Loads trait definitions from JSON at startup
- **`TraitSelectionValidator`**: Validates trait selections (eligibility, budget, conflicts)
- **`TraitDeathHandler`**: Handles trait behavior on character death
- **`TraitEffectApplicationService`**: Provides active effects for application at game boundary

## Testing

The system includes 35 comprehensive tests covering:
- Budget calculations
- Selection validation
- Confirmation workflow
- Death mechanics
- Effect application
- JSON loading
- Legacy character handling

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~BackgroundTraitTests"
```

## Best Practices

1. **Separate Definition from Selection**: Trait definitions are templates, CharacterTrait instances are selections
2. **Validate Before Confirming**: Always check budget and eligibility before finalizing
3. **Use Strong Types**: Prefer `RaceData` and `CharacterClassData` over raw strings
4. **Tag Effects**: Use `GenerateEffectTag()` to safely remove/reapply effects
5. **Only Apply Active Effects**: Check `IsConfirmed` and `IsActive` before applying
6. **Handle Death Properly**: Use `TraitDeathHandler` for consistent behavior
7. **Legacy Characters**: Grant default budget to characters without trait records

## Future Enhancements

- Trait categories (combat, social, magical, etc.)
- Tiered traits (basic → advanced → master)
- Conditional effects (active only in certain situations)
- Trait evolution (traits that change over time)
- Multi-point traits (cost 2+ points for powerful effects)
- Faction-specific traits
- Dynamic prerequisites (level requirements, quest completion)
