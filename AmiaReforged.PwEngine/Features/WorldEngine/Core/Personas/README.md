# Personas (Cross-Cutting)

Personas represent any actor in the WorldEngine - players, characters, organizations, governments, etc.

## Why Cross-Cutting?

Personas are used by **all subsystems**:
- Economy - Account ownership
- Organizations - Membership
- Characters - Identity resolution
- Industries - Permissions
- Codex - Knowledge tracking
- And more...

## Structure

```
Personas/
├── IPersonaGateway.cs           ← Public API
├── PersonaGateway.cs            ← Implementation
├── DTOs/                         ← Data transfer objects
│   ├── PersonaInfo.cs
│   ├── CharacterPersonaInfo.cs
│   ├── PlayerPersonaInfo.cs
│   └── CharacterIdentityInfo.cs
└── README.md                     ← This file
```

## Usage

### Access via WorldEngine Facade

```csharp
public class MyService
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public MyService(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task Example()
    {
        // Get all characters for a player
        var characters = await _worldEngine.Personas
            .GetPlayerCharactersAsync(cdKey);

        // Find who owns a character
        var owner = await _worldEngine.Personas
            .GetCharacterOwnerAsync(characterId);

        // Get character identity
        var identity = await _worldEngine.Personas
            .GetCharacterIdentityAsync(characterId);

        // Map IDs
        var personaId = await _worldEngine.Personas
            .GetCharacterPersonaIdAsync(characterId);
    }
}
```

## Operations

### Player-Character Mappings
- `GetPlayerCharactersAsync` - Get all characters for a player
- `GetCharacterOwnerAsync` - Find who owns a character
- `GetCharacterPersonaIdAsync` - CharacterId → PersonaId
- `GetPersonaCharacterIdAsync` - PersonaId → CharacterId

### Identity Information
- `GetCharacterIdentityAsync` - Full character identity
- `GetCharacterIdentityByPersonaAsync` - Identity by PersonaId
- `GetPlayerAsync` - Player information
- `GetPlayerByPersonaAsync` - Player by PersonaId

### Basic Operations
- `GetPersonaAsync` - Get a persona
- `GetPersonasAsync` - Get multiple personas
- `ExistsAsync` - Check if exists

### Holdings (Future)
- `GetPersonaHoldingsAsync` - Get persona holdings
- `GetPlayerAggregateHoldingsAsync` - Aggregate across characters

## What is a Persona?

A Persona is a universal identity representing any actor:

- **Players** - Identified by CD key
- **Characters** - Player-controlled actors
- **Organizations** - Guilds, factions
- **Governments** - Regional authorities
- **Coinhouses** - Banking institutions
- **System** - Automated actors

## Why Personas Matter

Instead of having separate "player ID", "character ID", "organization ID" everywhere, we have one universal `PersonaId` that can represent any actor. This simplifies:

- Permissions (who can do what?)
- Ownership (who owns this?)
- Relationships (who knows whom?)
- Transactions (who paid whom?)

## See Also

- [PERSONA_ARCHITECTURE_FINAL.md](../../PERSONA_ARCHITECTURE_FINAL.md) - Complete architecture
- [PERSONA_QUICK_REFERENCE.md](../../PERSONA_QUICK_REFERENCE.md) - Quick reference
- [CROSS_CUTTING_ARCHITECTURE.md](../../CROSS_CUTTING_ARCHITECTURE.md) - Why cross-cutting?

