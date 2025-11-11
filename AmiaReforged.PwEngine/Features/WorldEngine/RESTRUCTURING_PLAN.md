# WorldEngine Restructuring Plan

**Date:** November 10, 2025
**Status:** 📋 Proposed
**Goal:** Organize WorldEngine structure to be intuitive and easy to navigate

---

## Current Problems

### 1. **Scattered Organization**
```
WorldEngine/
├── Economy/              ← Domain folder with implementation details
├── Organizations/        ← Domain folder with implementation details
├── Characters/          ← Domain folder with implementation details
├── Subsystems/          ← Interfaces and implementations in separate folder
│   ├── IEconomySubsystem.cs
│   ├── Gateways/
│   └── Implementations/
├── IWorldEngineFacade.cs ← Top-level facade
└── WorldEngineFacade.cs
```

**Issues:**
- ❌ Domain folders (Economy, Organizations) contain implementation details
- ❌ Subsystem interfaces are in a separate `Subsystems/` folder
- ❌ Hard to find what belongs together
- ❌ No clear "this is the entry point" structure

### 2. **Unclear Relationships**
- Where do I find "Economy operations"?
  - In `Economy/` folder?
  - In `Subsystems/IEconomySubsystem.cs`?
  - In `Subsystems/Gateways/IBankingGateway.cs`?
- Answer: All three places! 😵

### 3. **Deep Nesting**
- `Subsystems/Implementations/Gateways/PersonaGateway.cs` - 4 levels deep!
- `Economy/Banks/Nui/BankWindowPresenter.cs` - 4 levels deep!

---

## Proposed Structure

### Organizing Principle
**"Keep things that change together, together"**

Each subsystem should be self-contained with:
- Its facade/interface
- Its gateways (public API)
- Its implementation (commands, queries, domain logic)
- Its tests

```
WorldEngine/
├── 📄 IWorldEngineFacade.cs           ← Top-level entry point (STAYS HERE)
├── 📄 WorldEngineFacade.cs            ← Facade implementation (STAYS HERE)
├── 📄 README.md                        ← Architecture overview
│
├── 📁 Core/                            ← NEW: Cross-cutting concerns
│   ├── 📁 Personas/                    ← Persona gateway (cross-cutting)
│   │   ├── IPersonaGateway.cs
│   │   ├── PersonaGateway.cs
│   │   ├── DTOs/
│   │   └── README.md
│   ├── 📁 SharedKernel/                ← Shared value objects, base classes
│   └── 📁 Infrastructure/              ← Common services, configs
│
├── 📁 Subsystems/                      ← NEW: All subsystems organized here
│   │
│   ├── 📁 Economy/                     ← Economy subsystem (SELF-CONTAINED)
│   │   ├── IEconomySubsystem.cs       ← Subsystem interface
│   │   ├── EconomySubsystem.cs        ← Subsystem implementation
│   │   ├── 📁 Gateways/                ← Public API
│   │   │   ├── IBankingGateway.cs
│   │   │   ├── IStorageGateway.cs
│   │   │   └── IShopGateway.cs
│   │   ├── 📁 Implementation/          ← Internal implementation
│   │   │   ├── 📁 Banking/
│   │   │   │   ├── BankingGateway.cs
│   │   │   │   ├── Commands/
│   │   │   │   ├── Queries/
│   │   │   │   └── Domain/
│   │   │   ├── 📁 Storage/
│   │   │   └── 📁 Shops/
│   │   ├── 📁 UI/                      ← UI components for this subsystem
│   │   │   └── Banks/
│   │   │       └── Nui/
│   │   ├── 📁 Tests/                   ← Tests for this subsystem
│   │   └── README.md                   ← Economy subsystem docs
│   │
│   ├── 📁 Organizations/               ← Organizations subsystem
│   │   ├── IOrganizationSubsystem.cs
│   │   ├── OrganizationSubsystem.cs
│   │   ├── 📁 Gateways/
│   │   ├── 📁 Implementation/
│   │   ├── 📁 Tests/
│   │   └── README.md
│   │
│   ├── 📁 Characters/                  ← Characters subsystem
│   │   ├── ICharacterSubsystem.cs
│   │   ├── CharacterSubsystem.cs
│   │   ├── 📁 Gateways/
│   │   ├── 📁 Implementation/
│   │   ├── 📁 Tests/
│   │   └── README.md
│   │
│   ├── 📁 Industries/                  ← Industries subsystem
│   │   ├── IIndustrySubsystem.cs
│   │   ├── IndustrySubsystem.cs
│   │   ├── 📁 Gateways/
│   │   ├── 📁 Implementation/
│   │   ├── 📁 Tests/
│   │   └── README.md
│   │
│   ├── 📁 Codex/                       ← Codex subsystem
│   │   └── ... (same pattern)
│   │
│   ├── 📁 Harvesting/
│   ├── 📁 Regions/
│   ├── 📁 Traits/
│   └── 📁 Items/
│
└── 📁 Documentation/                    ← All docs in one place
    ├── FACADE_GUIDE.md
    ├── CROSS_CUTTING_ARCHITECTURE.md
    ├── MIGRATION_GUIDES.md
    └── ... (all other .md files)
```

---

## Benefits of New Structure

### 1. **Clear Entry Points**
```
Want to work with Economy?
→ Go to Subsystems/Economy/
→ See IEconomySubsystem.cs (the public contract)
→ See Gateways/ (the public operations)
→ Implementation/ is internal details
```

### 2. **Self-Contained Subsystems**
Each subsystem folder contains:
- ✅ Its public interface (IXxxSubsystem)
- ✅ Its implementation
- ✅ Its gateways (public API)
- ✅ Its internal domain logic
- ✅ Its UI components
- ✅ Its tests
- ✅ Its documentation

### 3. **Flatter Structure**
```
Before: Subsystems/Implementations/Gateways/PersonaGateway.cs (4 levels)
After:  Core/Personas/PersonaGateway.cs (2 levels)

Before: Economy/Banks/Nui/BankWindowPresenter.cs (4 levels)
After:  Subsystems/Economy/UI/Banks/Nui/BankWindowPresenter.cs (5 levels, but clearer!)
```

### 4. **Logical Grouping**
- Cross-cutting concerns in `Core/`
- Domain subsystems in `Subsystems/`
- Documentation in `Documentation/`
- Facade at the root (it's the entry point!)

---

## Migration Strategy

### Phase 1: Create New Structure (No Breaking Changes)
1. Create `Core/` folder
2. Create `Subsystems/Economy/` folder structure
3. Create `Documentation/` folder

### Phase 2: Move Files (Iterative)
For each subsystem (starting with Economy):
1. Move subsystem interface to `Subsystems/Economy/IEconomySubsystem.cs`
2. Move implementation to `Subsystems/Economy/EconomySubsystem.cs`
3. Move gateways to `Subsystems/Economy/Gateways/`
4. Move implementation to `Subsystems/Economy/Implementation/`
5. Update namespaces
6. Build and test

### Phase 3: Update References
1. Update using statements throughout codebase
2. Update test references
3. Build and verify

### Phase 4: Clean Up
1. Remove old folders
2. Update documentation
3. Final verification

---

## Example: Economy Subsystem Structure

```
Subsystems/Economy/
├── IEconomySubsystem.cs                    ← Public interface
├── EconomySubsystem.cs                     ← Implementation
├── README.md                                ← "What is the Economy subsystem?"
│
├── Gateways/                                ← PUBLIC API
│   ├── IBankingGateway.cs
│   ├── IStorageGateway.cs
│   └── IShopGateway.cs
│
├── Implementation/                          ← INTERNAL (private to subsystem)
│   ├── Banking/
│   │   ├── BankingGateway.cs               ← Gateway implementation
│   │   ├── Commands/
│   │   │   ├── OpenCoinhouseAccountCommand.cs
│   │   │   ├── OpenCoinhouseAccountCommandHandler.cs
│   │   │   ├── DepositGoldCommand.cs
│   │   │   └── DepositGoldCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetCoinhouseAccountQuery.cs
│   │   │   └── GetCoinhouseAccountQueryHandler.cs
│   │   └── Domain/
│   │       ├── CoinhouseAccount.cs
│   │       ├── CoinhouseAccountEligibility.cs
│   │       └── BankAccessEvaluator.cs
│   │
│   ├── Storage/
│   │   ├── StorageGateway.cs
│   │   ├── Commands/
│   │   └── Queries/
│   │
│   └── Shops/
│       ├── ShopGateway.cs
│       ├── Commands/
│       └── Queries/
│
├── UI/                                      ← UI components for this subsystem
│   └── Banking/
│       └── Nui/
│           ├── BankWindowPresenter.cs
│           ├── BankWindowView.cs
│           └── BankAccountModel.cs
│
└── Tests/                                   ← Tests for this subsystem
    ├── Banking/
    │   ├── BankingGatewayTests.cs
    │   ├── Commands/
    │   └── Queries/
    └── Storage/
```

---

## Namespace Changes

### Before
```csharp
// Scattered across many namespaces
AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands
AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations
```

### After
```csharp
// Organized by subsystem
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Gateways
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banking
AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking
```

---

## Navigation Examples

### "I want to add a new banking feature"
```
1. Go to: Subsystems/Economy/
2. Look at: Gateways/IBankingGateway.cs (public API)
3. Implement in: Implementation/Banking/
4. Add UI in: UI/Banking/
5. Add tests in: Tests/Banking/
```

### "I want to understand how Economy works"
```
1. Go to: Subsystems/Economy/
2. Read: README.md (overview)
3. Look at: IEconomySubsystem.cs (what it does)
4. Look at: Gateways/ (how to use it)
```

### "I want to see all subsystems"
```
1. Go to: Subsystems/
2. See list:
   - Economy/
   - Organizations/
   - Characters/
   - Industries/
   - etc.
```

---

## Decision: Should We Do This?

### Pros ✅
- Much clearer structure
- Each subsystem is self-contained
- Easier to navigate
- Easier to onboard new developers
- Follows "vertical slice" architecture
- Consistent organization

### Cons ⚠️
- Requires moving many files
- Need to update all namespaces
- Need to update all references
- Takes time (but worthwhile!)

### Recommendation
**YES - Do the restructuring!**

The current structure is confusing. This will make the codebase much more maintainable long-term. The upfront cost is worth the long-term benefit.

---

## Implementation Plan

### Step 1: Backup
Create a git branch: `feature/worldengine-restructure`

### Step 2: Start with Economy (Proof of Concept)
1. Create `Subsystems/Economy/` structure
2. Move Economy files
3. Update namespaces
4. Build and test
5. Verify everything works

### Step 3: Repeat for Other Subsystems
Once Economy works, repeat the pattern for:
- Organizations
- Characters
- Industries
- etc.

### Step 4: Move Cross-Cutting Concerns
Move Personas to `Core/Personas/`

### Step 5: Consolidate Documentation
Move all .md files to `Documentation/`

### Step 6: Clean Up
Remove old empty folders

---

## Timeline Estimate

| Phase | Estimated Time |
|-------|---------------|
| Planning & setup | 1 hour |
| Economy subsystem | 2-3 hours |
| Organizations subsystem | 1-2 hours |
| Characters subsystem | 1-2 hours |
| Other subsystems | 3-4 hours |
| Testing & verification | 2 hours |
| **Total** | **10-14 hours** |

---

## Questions to Consider

1. **Should UI be in each subsystem or separate?**
   - **Recommendation:** In each subsystem (co-locate related code)

2. **Should tests be in each subsystem or separate?**
   - **Recommendation:** In each subsystem (easier to find relevant tests)

3. **Should we keep the current folder names or rename?**
   - **Recommendation:** Keep names (Economy, Organizations, etc.)

4. **When should we do this?**
   - **Recommendation:** Soon, before more code is added

---

## Success Criteria

After restructuring:
- ✅ Any developer can find a subsystem in <10 seconds
- ✅ Clear what's public API vs internal implementation
- ✅ Each subsystem is self-contained
- ✅ All builds pass
- ✅ All tests pass
- ✅ Documentation is updated

---

**Ready to proceed?** This will significantly improve the codebase organization!

