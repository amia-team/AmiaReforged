# WorldEngine Navigation Guide

**Purpose:** Quick reference for navigating the WorldEngine codebase

---

## Current Structure (As-Is)

### Finding Things Today

```
WorldEngine/
├── IWorldEngineFacade.cs              ← Start here (entry point)
├── WorldEngineFacade.cs
│
├── Subsystems/                         ← Subsystem interfaces
│   ├── IEconomySubsystem.cs
│   ├── IOrganizationSubsystem.cs
│   ├── Gateways/                       ← Gateway interfaces
│   │   ├── IBankingGateway.cs
│   │   ├── IPersonaGateway.cs
│   │   └── ...
│   └── Implementations/                ← Gateway implementations
│       ├── EconomySubsystem.cs
│       └── Gateways/
│           ├── BankingGateway.cs
│           ├── PersonaGateway.cs
│           └── ...
│
├── Economy/                            ← Domain implementation
│   ├── Banks/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── Nui/                        ← UI
│   ├── Shops/
│   └── Storage/
│
├── Organizations/                      ← Domain implementation
├── Characters/                         ← Domain implementation
└── ... (other domains)
```

### The Problem

To understand "Economy", you need to look in 3 places:
1. `Subsystems/IEconomySubsystem.cs` - What it does
2. `Subsystems/Implementations/EconomySubsystem.cs` - How it's wired
3. `Economy/` - The actual implementation

**This is confusing! 😵**

---

## Proposed Structure (To-Be)

### Finding Things (After Reorganization)

```
WorldEngine/
├── IWorldEngineFacade.cs              ← Entry point
├── WorldEngineFacade.cs
│
├── Core/                               ← Cross-cutting (used by everyone)
│   └── Personas/
│       ├── IPersonaGateway.cs
│       ├── PersonaGateway.cs
│       ├── DTOs/
│       └── README.md                   ← "What are Personas?"
│
└── Subsystems/                         ← All subsystems organized here
    │
    ├── Economy/                        ← Everything Economy in one place!
    │   ├── IEconomySubsystem.cs       ← Public interface
    │   ├── EconomySubsystem.cs        ← Implementation
    │   ├── README.md                   ← "What is Economy subsystem?"
    │   │
    │   ├── Gateways/                   ← PUBLIC API
    │   │   ├── IBankingGateway.cs
    │   │   ├── IStorageGateway.cs
    │   │   └── IShopGateway.cs
    │   │
    │   ├── Implementation/             ← INTERNAL
    │   │   ├── Banking/
    │   │   │   ├── BankingGateway.cs
    │   │   │   ├── Commands/
    │   │   │   ├── Queries/
    │   │   │   └── Domain/
    │   │   ├── Storage/
    │   │   └── Shops/
    │   │
    │   ├── UI/                         ← UI for this subsystem
    │   │   └── Banking/
    │   │       └── Nui/
    │   │
    │   └── Tests/                      ← Tests for this subsystem
    │       ├── Banking/
    │       └── Storage/
    │
    ├── Organizations/                  ← Same pattern
    │   ├── IOrganizationSubsystem.cs
    │   ├── OrganizationSubsystem.cs
    │   ├── README.md
    │   ├── Gateways/
    │   ├── Implementation/
    │   ├── UI/
    │   └── Tests/
    │
    └── ... (all other subsystems follow same pattern)
```

### The Solution

To understand "Economy", look in ONE place:
- `Subsystems/Economy/` - Everything is here!
  - `IEconomySubsystem.cs` - What it does
  - `Gateways/` - How to use it
  - `Implementation/` - How it works internally
  - `README.md` - Documentation

**This is clear! ✅**

---

## Navigation Cheat Sheet

### "I want to use the WorldEngine"
```
📂 Root
├── IWorldEngineFacade.cs     ← Look here first
└── README.md                  ← Architecture overview
```

### "I want to work with Economy"
```
📂 Subsystems/Economy/
├── README.md                  ← Start here (overview)
├── IEconomySubsystem.cs      ← Public contract
├── Gateways/                  ← Public API
│   ├── IBankingGateway.cs    ← Banking operations
│   ├── IStorageGateway.cs    ← Storage operations
│   └── IShopGateway.cs       ← Shop operations
└── Implementation/            ← Internal details
    ├── Banking/
    ├── Storage/
    └── Shops/
```

### "I want to add a new banking feature"
```
📂 Subsystems/Economy/
├── Gateways/
│   └── IBankingGateway.cs    ← 1. Add to public API
└── Implementation/Banking/
    ├── BankingGateway.cs     ← 2. Implement in gateway
    ├── Commands/              ← 3. Add command if needed
    └── Queries/               ← 4. Add query if needed
```

### "I want to understand Personas"
```
📂 Core/Personas/
├── README.md                  ← Start here
├── IPersonaGateway.cs        ← Public API
├── PersonaGateway.cs         ← Implementation
└── DTOs/                      ← Data transfer objects
```

### "I want to see all subsystems"
```
📂 Subsystems/
├── Economy/         ← Banking, shops, storage
├── Organizations/   ← Guilds, factions
├── Characters/      ← Character management
├── Industries/      ← Crafting
├── Harvesting/      ← Resource gathering
├── Regions/         ← Area management
├── Traits/          ← Character traits
├── Items/           ← Item definitions
└── Codex/           ← Knowledge system
```

---

## Mental Model

### Layer 1: Entry Point
```
IWorldEngineFacade
    ↓
"I want to do something in the world"
```

### Layer 2: Choose Subsystem
```
IWorldEngineFacade
    ├→ Economy          (financial operations)
    ├→ Organizations    (guild management)
    ├→ Characters       (character operations)
    └→ ...
```

### Layer 3: Use Gateway
```
Economy
    ├→ Banking          (bank accounts, deposits, withdrawals)
    ├→ Storage          (item storage, capacity)
    └→ Shops            (NPC shops, player stalls)
```

### Layer 4: Execute Operation
```
Banking.DepositGoldAsync(command)
Banking.WithdrawGoldAsync(command)
Banking.GetBalanceAsync(query)
```

---

## Key Principles

### 1. **Self-Contained Subsystems**
Each subsystem folder has EVERYTHING related to that domain:
- Interface
- Implementation
- Gateways
- Tests
- Documentation
- UI components

### 2. **Public vs Private**
- `Gateways/` = PUBLIC (what others can use)
- `Implementation/` = PRIVATE (internal details)

### 3. **Cross-Cutting vs Domain**
- `Core/` = Used by everyone (Personas, SharedKernel)
- `Subsystems/` = Domain-specific (Economy, Organizations)

### 4. **Flat is Better Than Nested**
- Avoid deep nesting (max 3-4 levels)
- Group by feature, not by pattern

---

## Quick Reference

| I want to... | Look in... |
|-------------|-----------|
| Use WorldEngine | `IWorldEngineFacade.cs` |
| Understand Economy | `Subsystems/Economy/README.md` |
| Add banking feature | `Subsystems/Economy/Implementation/Banking/` |
| Use banking operations | `Subsystems/Economy/Gateways/IBankingGateway.cs` |
| Work with personas | `Core/Personas/` |
| Find tests | `Subsystems/[SubsystemName]/Tests/` |
| Read docs | `Documentation/` |

---

## Benefits

### Before Reorganization
- ❌ Scattered files
- ❌ Hard to find related code
- ❌ Unclear what's public vs private
- ❌ Deep nesting (4+ levels)

### After Reorganization
- ✅ Self-contained subsystems
- ✅ Everything in logical place
- ✅ Clear public API (Gateways/)
- ✅ Flatter structure
- ✅ Easy to navigate
- ✅ Consistent organization

---

## See Also

- [RESTRUCTURING_PLAN.md](./RESTRUCTURING_PLAN.md) - Detailed migration plan
- [FACADE_GUIDE.md](./FACADE_GUIDE.md) - How to use the facade
- [CROSS_CUTTING_ARCHITECTURE.md](./CROSS_CUTTING_ARCHITECTURE.md) - Architecture principles

---

**Summary:** The proposed reorganization makes the codebase **significantly easier to navigate** by keeping related code together and following consistent patterns.

