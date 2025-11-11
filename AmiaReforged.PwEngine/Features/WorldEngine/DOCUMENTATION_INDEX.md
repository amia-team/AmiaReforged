# WorldEngine Documentation Index

**Last Updated:** November 10, 2025

This index provides quick access to all WorldEngine documentation organized by topic.

---

## 🏗️ Architecture & Design

### Core Architecture
- **[README.md](./README.md)** - WorldEngine overview and getting started
- **[CROSS_CUTTING_ARCHITECTURE.md](./CROSS_CUTTING_ARCHITECTURE.md)** - Cross-cutting concerns vs domain subsystems

### Facade Pattern
- **[FACADE_GUIDE.md](./FACADE_GUIDE.md)** - Complete guide to using the WorldEngine facade
- **[FACADE_QUICK_REFERENCE.md](./FACADE_QUICK_REFERENCE.md)** - Quick reference card for common operations
- **[FACADE_ANALYSIS_COMPLETE.md](./FACADE_ANALYSIS_COMPLETE.md)** - Analysis of current facade usage
- **[FACADE_MIGRATION_PLAN.md](./FACADE_MIGRATION_PLAN.md)** - Plan for migrating remaining code
- **[FACADE_IMPLEMENTATION_SUMMARY.md](./FACADE_IMPLEMENTATION_SUMMARY.md)** - Implementation details
- **[FACADE_COMPLETE.md](./FACADE_COMPLETE.md)** - Completion status and achievements

---

## 👤 Personas System

### Overview & Architecture
- **[PERSONA_ARCHITECTURE_FINAL.md](./PERSONA_ARCHITECTURE_FINAL.md)** - Complete persona architecture summary
- **[PERSONA_GATEWAY_COMPLETE.md](./Subsystems/PERSONA_GATEWAY_COMPLETE.md)** - PersonaGateway implementation guide
- **[PERSONA_QUICK_REFERENCE.md](./PERSONA_QUICK_REFERENCE.md)** - Quick reference for persona operations

### Key Concepts
- **Cross-Cutting Concern** - Personas are at WorldEngine level, not economy-specific
- **Actor Representation** - Players, characters, organizations, governments, etc.
- **Identity Resolution** - Character-to-player mappings, ownership tracking
- **Universal Usage** - Used by all subsystems

### Usage
```csharp
// Access personas at WorldEngine level
var characters = await _worldEngine.Personas.GetPlayerCharactersAsync(cdKey);
var owner = await _worldEngine.Personas.GetCharacterOwnerAsync(characterId);
var identity = await _worldEngine.Personas.GetCharacterIdentityAsync(characterId);
```

---

## 💰 Economy System

### Overview
- **[EconomyGameDesign.MD](./EconomyGameDesign.MD)** - Economy system design document
- **[ECONOMY_GATEWAY_REFACTORING.md](./Subsystems/ECONOMY_GATEWAY_REFACTORING.md)** - Economy gateway architecture

### Gateways
- **IBankingGateway** - Account management, deposits, withdrawals
- **IStorageGateway** - Item storage, capacity management
- **IShopGateway** - Player stalls, NPC shops

### Usage
```csharp
// Banking operations
await _worldEngine.Economy.Banking.OpenCoinhouseAccountAsync(command);
await _worldEngine.Economy.Banking.DepositGoldAsync(command);
var balance = await _worldEngine.Economy.Banking.GetBalanceAsync(query);

// Storage operations
await _worldEngine.Economy.Storage.StoreItemAsync(command);
var items = await _worldEngine.Economy.Storage.GetStoredItemsAsync(query);

// Shop operations
await _worldEngine.Economy.Shops.ClaimPlayerStallAsync(command);
```

---

## 🏛️ Organizations System

### Overview
Organizations represent guilds, factions, governments, and other collective entities.

### Operations
- Organization management (create, disband, update)
- Membership management (add, remove, promote)
- Queries (details, members, character organizations)

### Usage
```csharp
await _worldEngine.Organizations.CreateOrganizationAsync(command);
var org = await _worldEngine.Organizations.GetOrganizationDetailsAsync(query);
await _worldEngine.Organizations.AddMemberAsync(orgId, characterId, rank);
```

---

## 🎯 Characters System

### Overview
Character registration, stats, reputation, and progression tracking.

### Operations
- Character registration
- Stats management
- Reputation tracking
- Knowledge contexts

### Usage
```csharp
await _worldEngine.Characters.RegisterCharacterAsync(characterId);
await _worldEngine.Characters.AdjustReputationAsync(characterId, orgId, change, reason);
var reputation = await _worldEngine.Characters.GetReputationAsync(characterId, orgId);
```

---

## 🔨 Industries System

### Overview
Crafting, recipes, industry membership, and production.

### Operations
- Industry queries
- Crafting operations
- Recipe management
- Membership and learning

### Usage
```csharp
var industries = await _worldEngine.Industries.GetAllIndustriesAsync();
await _worldEngine.Industries.CraftItemAsync(command);
await _worldEngine.Industries.LearnRecipeAsync(characterId, recipeId);
```

---

## 📚 Codex System

### Overview
Knowledge management, lore tracking, discoveries.

### Operations
- Knowledge entries
- Character knowledge tracking
- Lore management

### Usage
```csharp
await _worldEngine.Codex.RecordDiscoveryAsync(personaId, loreId);
var knowledge = await _worldEngine.Codex.GetCharacterKnowledgeAsync(characterId);
```

---

## 🧪 Testing

### Test Helpers
- **[PersonaTestHelpers.cs](./Tests/Helpers/WorldEngine/PersonaTestHelpers.cs)** - Helpers for creating test personas
- **[EconomyTestHelpers.cs](./Tests/Helpers/WorldEngine/EconomyTestHelpers.cs)** - Helpers for economy tests

### Test Examples
```csharp
[Test]
public async Task TestBankingOperation()
{
    // Arrange
    var mockBanking = new Mock<IBankingGateway>();
    mockBanking.Setup(b => b.DepositGoldAsync(It.IsAny<DepositGoldCommand>(), default))
        .ReturnsAsync(CommandResult.Ok());

    var mockEconomy = new Mock<IEconomySubsystem>();
    mockEconomy.Setup(e => e.Banking).Returns(mockBanking.Object);

    var mockFacade = new Mock<IWorldEngineFacade>();
    mockFacade.Setup(f => f.Economy).Returns(mockEconomy.Object);

    // Act & Assert
    var service = new BankManager(mockFacade.Object);
    await service.ProcessDeposit(personaId, 100);

    mockBanking.Verify(b => b.DepositGoldAsync(
        It.Is<DepositGoldCommand>(c => c.Amount.Value == 100),
        default),
        Times.Once);
}
```

---

## 🚀 Quick Start

### For New Developers

1. **Read** [FACADE_GUIDE.md](./FACADE_GUIDE.md) - Understand the facade pattern
2. **Read** [CROSS_CUTTING_ARCHITECTURE.md](./CROSS_CUTTING_ARCHITECTURE.md) - Understand architecture layers
3. **Read** [PERSONA_QUICK_REFERENCE.md](./PERSONA_QUICK_REFERENCE.md) - Learn persona operations
4. **Review** examples in this index
5. **Start coding!**

### Common Patterns

#### Accessing WorldEngine
```csharp
public class MyService
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public MyService(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }
}
```

#### Working with Personas
```csharp
// Get character identity
var identity = await _worldEngine.Personas.GetCharacterIdentityAsync(characterId);

// Get player's characters
var characters = await _worldEngine.Personas.GetPlayerCharactersAsync(cdKey);

// Get character owner
var owner = await _worldEngine.Personas.GetCharacterOwnerAsync(characterId);
```

#### Economy Operations
```csharp
// Banking
await _worldEngine.Economy.Banking.DepositGoldAsync(command);

// Storage
await _worldEngine.Economy.Storage.StoreItemAsync(command);

// Shops
await _worldEngine.Economy.Shops.ClaimPlayerStallAsync(command);
```

#### Cross-Subsystem Operations
```csharp
// 1. Get character identity
var identity = await _worldEngine.Personas.GetCharacterIdentityAsync(characterId);

// 2. Use in economy operation
await _worldEngine.Economy.Banking.DepositGoldAsync(
    DepositGoldCommand.Create(identity.PersonaId, coinhouse, amount, memo));

// 3. Update reputation
await _worldEngine.Characters.AdjustReputationAsync(
    characterId, organizationId, change, reason);
```

---

## 📊 System Status

| Subsystem | Status | Documentation |
|-----------|--------|---------------|
| **Personas** | ✅ Complete | [PERSONA_ARCHITECTURE_FINAL.md](./PERSONA_ARCHITECTURE_FINAL.md) |
| **Economy** | ✅ Complete | [ECONOMY_GATEWAY_REFACTORING.md](./Subsystems/ECONOMY_GATEWAY_REFACTORING.md) |
| **Organizations** | 🟡 Partial | See [FACADE_GUIDE.md](./FACADE_GUIDE.md) |
| **Characters** | ✅ Complete | See [FACADE_GUIDE.md](./FACADE_GUIDE.md) |
| **Industries** | 🟡 Partial | See [FACADE_GUIDE.md](./FACADE_GUIDE.md) |
| **Harvesting** | 🔴 Stub | TBD |
| **Regions** | 🔴 Stub | TBD |
| **Traits** | 🔴 Stub | TBD |
| **Items** | 🔴 Stub | TBD |
| **Codex** | 🔴 Stub | TBD |

---

## 🔧 Migration & Maintenance

### Active Migrations
- **[FACADE_MIGRATION_PLAN.md](./FACADE_MIGRATION_PLAN.md)** - BankWindowPresenter refactoring

### Maintenance Tasks
- Keep documentation up to date
- Add examples as new patterns emerge
- Update status indicators
- Review and improve architecture

---

## 💡 Best Practices

### DO ✅
- Inject `IWorldEngineFacade` or specific subsystems
- Use personas for actor identity
- Use gateways for domain operations
- Write tests that mock subsystems
- Follow established patterns

### DON'T ❌
- Inject individual command/query handlers
- Bypass the facade pattern
- Mix cross-cutting concerns with domain logic
- Create tight coupling to implementation details
- Ignore existing patterns

---

## 📞 Getting Help

### Documentation Issues
- Check this index for the right document
- Use CTRL+F to search within documents
- Follow links to related topics

### Code Issues
- Review relevant subsystem documentation
- Check examples in FACADE_GUIDE.md
- Look at existing tests for patterns
- Consult CROSS_CUTTING_ARCHITECTURE.md for design principles

### New Features
- Understand existing patterns first
- Follow the facade pattern
- Add to appropriate subsystem
- Update documentation
- Write tests

---

## 📝 Document Conventions

### Status Indicators
- ✅ **Complete** - Fully implemented and tested
- 🟡 **Partial** - Core features working, some pending
- 🔴 **Stub** - Interface exists, needs implementation
- 🎯 **Priority** - High priority for completion
- 🔜 **Planned** - Scheduled for future work

### File Organization
- **Root** - Architecture and facade docs
- **Subsystems/** - Subsystem-specific guides
- **Tests/** - Test documentation and helpers

---

**Questions?** Start with [FACADE_GUIDE.md](./FACADE_GUIDE.md) for the big picture!

