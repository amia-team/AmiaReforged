# Persona Gateway - Quick Reference

## Import

```csharp
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;
```

## Access

```csharp
// Inject WorldEngineFacade
private readonly IWorldEngineFacade _worldEngine;

// Access personas at WorldEngine level
_worldEngine.Personas
```

## Common Operations

### Get All Characters for a Player
```csharp
IReadOnlyList<CharacterPersonaInfo> characters =
    await _worldEngine.Personas.GetPlayerCharactersAsync(cdKey);
```

### Find Character Owner
```csharp
PlayerPersonaInfo? owner =
    await _worldEngine.Personas.GetCharacterOwnerAsync(characterId);
```

### Get Character Identity
```csharp
CharacterIdentityInfo? identity =
    await _worldEngine.Personas.GetCharacterIdentityAsync(characterId);
```

### Get Player Info
```csharp
PlayerPersonaInfo? player =
    await _worldEngine.Personas.GetPlayerAsync(cdKey);
```

### ID Conversions
```csharp
// CharacterId → PersonaId
PersonaId? personaId =
    await _worldEngine.Personas.GetCharacterPersonaIdAsync(characterId);

// PersonaId → CharacterId
CharacterId? characterId =
    await _worldEngine.Personas.GetPersonaCharacterIdAsync(personaId);
```

### Check Existence
```csharp
bool exists = await _worldEngine.Personas.ExistsAsync(personaId);
```

## DTOs

### CharacterPersonaInfo
```csharp
record CharacterPersonaInfo : PersonaInfo
{
    PersonaId Id
    PersonaType Type
    string DisplayName
    CharacterId CharacterId
    string CdKey
}
```

### PlayerPersonaInfo
```csharp
record PlayerPersonaInfo : PersonaInfo
{
    PersonaId Id
    PersonaType Type
    string DisplayName
    string CdKey
    DateTime? LastSeenUtc
    int CharacterCount
}
```

### CharacterIdentityInfo
```csharp
record CharacterIdentityInfo
{
    CharacterId CharacterId
    PersonaId PersonaId
    string FirstName
    string? LastName
    string FullName
    string Description
    string CdKey
}
```

## Full Example

```csharp
public class ExampleService
{
    private readonly IWorldEngineFacade _worldEngine;

    public async Task ProcessPlayer(string cdKey)
    {
        // 1. Get player info
        var player = await _worldEngine.Personas.GetPlayerAsync(cdKey);
        if (player == null) return;

        Console.WriteLine($"Player: {player.DisplayName}");
        Console.WriteLine($"Characters: {player.CharacterCount}");

        // 2. Get all characters
        var characters = await _worldEngine.Personas
            .GetPlayerCharactersAsync(cdKey);

        foreach (var character in characters)
        {
            // 3. Get detailed identity
            var identity = await _worldEngine.Personas
                .GetCharacterIdentityAsync(character.CharacterId);

            Console.WriteLine($"  - {identity.FullName}");

            // 4. Use with other subsystems
            var accounts = await _worldEngine.Economy.Banking
                .GetCoinhouseBalancesAsync(
                    new GetCoinhouseBalancesQuery(identity.PersonaId));
        }
    }
}
```

## Architecture

```
WorldEngineFacade
    └── Personas (Cross-Cutting)
        ├── Used by Economy
        ├── Used by Organizations
        ├── Used by Characters
        ├── Used by Industries
        └── Used by ALL subsystems
```

## Location

- **Interface:** `Subsystems/Gateways/IPersonaGateway.cs`
- **Implementation:** `Subsystems/Implementations/Gateways/PersonaGateway.cs`
- **Tests:** `Tests/Systems/WorldEngine/Personas/PersonaGatewayTests.cs`
- **Docs:** `PERSONA_GATEWAY_COMPLETE.md`, `CROSS_CUTTING_ARCHITECTURE.md`

## Remember

✅ Personas are at **WorldEngine level**, not Economy
✅ Access via `_worldEngine.Personas`, not `_economy.Personas`
✅ Used by **all subsystems** for actor identity
✅ 20 tests, all passing ✅

