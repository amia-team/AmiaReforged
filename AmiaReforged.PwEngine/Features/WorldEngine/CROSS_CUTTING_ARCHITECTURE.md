# WorldEngine Architecture - Cross-Cutting Concerns

## Overview

The WorldEngine has both **subsystems** (domain-specific) and **cross-cutting gateways** (shared across all domains). This document explains the architecture and design decisions.

## Architecture Layers

```
┌─────────────────────────────────────────────┐
│         IWorldEngineFacade                  │
│  (Unified access point for all systems)    │
├─────────────────────────────────────────────┤
│                                             │
│  CROSS-CUTTING GATEWAYS                     │
│  (Used by all subsystems)                   │
│                                             │
│  • Personas (IPersonaGateway)              │
│    - Identity resolution                    │
│    - Character-player mappings              │
│    - Ownership tracking                     │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  DOMAIN SUBSYSTEMS                          │
│  (Specialized business logic)               │
│                                             │
│  • Economy                                  │
│    - Banking (IBankingGateway)             │
│    - Storage (IStorageGateway)             │
│    - Shops (IShopGateway)                  │
│                                             │
│  • Organizations                            │
│  • Characters                               │
│  • Industries                               │
│  • Harvesting                               │
│  • Regions                                  │
│  • Traits                                   │
│  • Items                                    │
│  • Codex                                    │
│                                             │
└─────────────────────────────────────────────┘
```

## Design Principles

### 1. Cross-Cutting Concerns at WorldEngine Level

**What are Cross-Cutting Concerns?**
- Functionality needed by multiple subsystems
- Not specific to any single domain
- Fundamental to the entire world engine

**Current Cross-Cutting Gateways:**
- **IPersonaGateway** - Identity and actor representation

**Future Cross-Cutting Gateways:**
- Authentication/Authorization
- Logging/Auditing
- Event Bus
- Caching
- Localization

### 2. Domain Subsystems for Specialized Logic

**What are Domain Subsystems?**
- Focused on specific business domains
- May have their own internal gateways
- Can depend on cross-cutting concerns

**Current Domain Subsystems:**
- **Economy** - Financial operations, shops, storage
- **Organizations** - Guilds, factions, alliances
- **Characters** - Registration, stats, progression
- **Industries** - Crafting, recipes, production
- **Harvesting** - Resource gathering
- **Regions** - Area management, governance
- **Traits** - Character traits and effects
- **Items** - Item definitions and properties
- **Codex** - Knowledge and lore tracking

## Why Personas are Cross-Cutting

### Used Throughout the WorldEngine

```csharp
// Economy uses personas for account ownership
var account = await worldEngine.Economy.Banking.OpenCoinhouseAccountAsync(
    new OpenCoinhouseAccountCommand
    {
        PersonaId = characterPersona.Id  // ← Persona
    });

// Organizations use personas for membership
await worldEngine.Organizations.AddMemberAsync(
    organizationId,
    personaId);  // ← Persona

// Industries use personas for access control
await worldEngine.Industries.LearnRecipeAsync(
    personaId,  // ← Persona
    recipeId);

// Codex uses personas for knowledge tracking
await worldEngine.Codex.RecordDiscoveryAsync(
    personaId,  // ← Persona
    loreId);

// Regions use personas for governance
await worldEngine.Regions.SetGovernorAsync(
    regionId,
    personaId);  // ← Persona
```

### Represents Universal Actor Concept

Personas can be:
- **Players** (identified by CD key)
- **Characters** (player-controlled actors)
- **Organizations** (guilds, factions)
- **Governments** (regional authorities)
- **Coinhouses** (banking institutions)
- **System Processes** (automated actors)

## Access Patterns

### ✅ Correct: Cross-Cutting at WorldEngine Level

```csharp
public class AnyService
{
    private readonly IWorldEngineFacade _worldEngine;

    public async Task DoWork()
    {
        // Access personas directly from WorldEngine
        var characters = await _worldEngine.Personas
            .GetPlayerCharactersAsync(cdKey);

        // Then use domain subsystems
        await _worldEngine.Economy.Banking.DepositGoldAsync(...);
        await _worldEngine.Organizations.CreateOrganizationAsync(...);
    }
}
```

### ❌ Incorrect: Nesting Under Domain Subsystems

```csharp
// DON'T do this - personas aren't economy-specific!
var characters = await _worldEngine.Economy.Personas
    .GetPlayerCharactersAsync(cdKey);

// This incorrectly implies personas are an economy concern
// when they're actually used by ALL subsystems
```

## Benefits of This Architecture

### 1. Clear Separation of Concerns
- Cross-cutting concerns are immediately identifiable
- Domain logic stays focused
- No confusion about where functionality belongs

### 2. Reusability
- Persona logic isn't duplicated across subsystems
- Single source of truth for identity operations
- Consistent behavior everywhere

### 3. Discoverability
- Developers know where to look for universal functionality
- Facade pattern makes all systems easily accessible
- Clear mental model of the architecture

### 4. Maintainability
- Changes to persona logic happen in one place
- Easy to add new cross-cutting concerns
- Subsystems remain independent

### 5. Testability
- Cross-cutting concerns can be mocked easily
- Subsystems can be tested in isolation
- Clear dependency graph

## Adding New Cross-Cutting Concerns

When adding a new cross-cutting gateway:

1. **Create the interface** in `Subsystems/Gateways/`
2. **Create the implementation** in `Subsystems/Implementations/Gateways/`
3. **Add to IWorldEngineFacade** in the "Cross-Cutting Gateways" section
4. **Add to WorldEngineFacade constructor** and property
5. **Document** why it's cross-cutting vs. domain-specific
6. **Write tests** covering all subsystem integrations

### Example: Adding an Audit Gateway

```csharp
public interface IWorldEngineFacade
{
    // === Cross-Cutting Gateways ===

    IPersonaGateway Personas { get; }
    IAuditGateway Audit { get; }  // ← NEW cross-cutting concern

    // === Subsystems ===
    IEconomySubsystem Economy { get; }
    // ... etc
}
```

## Migration Guide

If you have existing code accessing personas through economy:

### Before
```csharp
var characters = await _economy.Personas.GetPlayerCharactersAsync(cdKey);
```

### After
```csharp
var characters = await _worldEngine.Personas.GetPlayerCharactersAsync(cdKey);
```

## Summary

✅ **Cross-Cutting Concerns** → WorldEngine level
✅ **Domain Logic** → Subsystems
✅ **Personas** → Cross-cutting (used everywhere)
✅ **Economy/Banking/etc** → Domain-specific subsystems

This architecture ensures clarity, reusability, and maintainability across the entire WorldEngine!

