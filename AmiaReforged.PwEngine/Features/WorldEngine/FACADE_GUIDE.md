# WorldEngine Facade

**Version**: 1.0
**Status**: ✅ Ready for Use
**Last Updated**: November 10, 2025

---

## Overview

The **WorldEngine Facade** provides a unified, simplified interface to access all WorldEngine subsystems. Instead of injecting individual command and query handlers, you can inject a single `IWorldEngineFacade` that gives you organized access to all WorldEngine functionality.

---

## Why Use the Facade?

### Before (Without Facade)

```csharp
public class MyService
{
    private readonly ICommandHandler<DepositGoldCommand> _depositHandler;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;
    private readonly IQueryHandler<GetBalanceQuery, int> _balanceHandler;
    private readonly ICommandHandler<CreateOrganizationCommand> _createOrgHandler;
    private readonly IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> _getOrgHandler;
    private readonly ICommandHandler<CraftItemCommand> _craftHandler;
    // ... many more handlers

    public MyService(
        ICommandHandler<DepositGoldCommand> depositHandler,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler,
        IQueryHandler<GetBalanceQuery, int> balanceHandler,
        ICommandHandler<CreateOrganizationCommand> createOrgHandler,
        IQueryHandler<GetOrganizationDetailsQuery, IOrganization?> getOrgHandler,
        ICommandHandler<CraftItemCommand> craftHandler
        // ... many more parameters
    )
    {
        _depositHandler = depositHandler;
        _withdrawHandler = withdrawHandler;
        _balanceHandler = balanceHandler;
        _createOrgHandler = createOrgHandler;
        _getOrgHandler = getOrgHandler;
        _craftHandler = craftHandler;
    }

    public async Task DoSomething()
    {
        var balance = await _balanceHandler.HandleAsync(query);
        await _depositHandler.HandleAsync(command);
    }
}
```

### After (With Facade)

```csharp
public class MyService
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public MyService(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task DoSomething()
    {
        var balance = await _worldEngine.Economy.GetBalanceAsync(query);
        await _worldEngine.Economy.DepositGoldAsync(command);
    }
}
```

---

## Architecture

```
┌─────────────────────────────────────────┐
│       IWorldEngineFacade                │
│  (Single Entry Point)                   │
└────────────┬────────────────────────────┘
             │
             ├── IEconomySubsystem
             │   ├── Banking
             │   ├── Transactions
             │   └── Storage
             │
             ├── IOrganizationSubsystem
             │   ├── Management
             │   ├── Membership
             │   └── Queries
             │
             ├── ICharacterSubsystem
             │   ├── Registration
             │   ├── Stats
             │   └── Reputation
             │
             ├── IIndustrySubsystem
             │   ├── Crafting
             │   ├── Recipes
             │   └── Membership
             │
             ├── IHarvestingSubsystem
             │   ├── Resource Nodes
             │   └── Gathering
             │
             ├── IRegionSubsystem
             │   ├── Area Management
             │   └── Regional Effects
             │
             ├── ITraitSubsystem
             │   ├── Character Traits
             │   └── Trait Effects
             │
             ├── IItemSubsystem
             │   └── Item Definitions
             │
             └── ICodexSubsystem
                 └── Knowledge Management
```

---

## Usage Examples

### Economy Operations

```csharp
[ServiceBinding(typeof(BankManager))]
public class BankManager
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public BankManager(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task ProcessDeposit(PersonaId personaId, int amount)
    {
        // Create command
        var command = DepositGoldCommand.Create(
            personaId,
            CoinhouseTag.Parse("cordor_bank"),
            amount,
            "Player deposit");

        // Execute via facade
        var result = await _worldEngine.Economy.DepositGoldAsync(command);

        if (result.Success)
        {
            Console.WriteLine("Deposit successful!");
        }
        else
        {
            Console.WriteLine($"Deposit failed: {result.ErrorMessage}");
        }
    }

    public async Task<int> CheckBalance(PersonaId personaId)
    {
        var query = new GetBalanceQuery
        {
            PersonaId = personaId,
            Coinhouse = CoinhouseTag.Parse("cordor_bank")
        };

        return await _worldEngine.Economy.GetBalanceAsync(query);
    }
}
```

### Organization Operations

```csharp
public class GuildSystem
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public GuildSystem(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task CreateGuild(string name, string description, CharacterId founderId)
    {
        var command = new CreateOrganizationCommand
        {
            Name = name,
            Description = description,
            Type = OrganizationType.Guild,
            FounderId = founderId
        };

        var result = await _worldEngine.Organizations.CreateOrganizationAsync(command);

        if (result.Success)
        {
            var orgId = result.Data?["OrganizationId"] as OrganizationId;
            Console.WriteLine($"Guild created with ID: {orgId}");
        }
    }

    public async Task<IOrganization?> GetGuildDetails(OrganizationId guildId)
    {
        var query = new GetOrganizationDetailsQuery
        {
            OrganizationId = guildId
        };

        return await _worldEngine.Organizations.GetOrganizationDetailsAsync(query);
    }
}
```

### Industry/Crafting Operations

```csharp
public class CraftingService
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public CraftingService(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task CraftItem(CharacterId characterId, string recipeId)
    {
        // Check if character knows the industry
        var memberships = await _worldEngine.Industries
            .GetCharacterIndustriesAsync(characterId);

        if (!memberships.Any())
        {
            Console.WriteLine("Character is not enrolled in any industries!");
            return;
        }

        // Attempt to craft
        var command = new CraftItemCommand
        {
            CharacterId = characterId,
            RecipeId = recipeId,
            IndustryTag = memberships[0].IndustryTag
        };

        var result = await _worldEngine.Industries.CraftItemAsync(command);

        if (result.Success)
        {
            Console.WriteLine("Item crafted successfully!");
        }
    }
}
```

### Character Operations

```csharp
public class CharacterManager
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public CharacterManager(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task RegisterNewCharacter(CharacterId characterId)
    {
        var result = await _worldEngine.Characters.RegisterCharacterAsync(characterId);

        if (result.Success)
        {
            Console.WriteLine("Character registered in WorldEngine!");
        }
    }

    public async Task AdjustReputation(
        CharacterId characterId,
        OrganizationId organizationId,
        int change)
    {
        await _worldEngine.Characters.AdjustReputationAsync(
            characterId,
            organizationId,
            change,
            "Quest reward");

        var newReputation = await _worldEngine.Characters
            .GetReputationAsync(characterId, organizationId);

        Console.WriteLine($"New reputation: {newReputation}");
    }
}
```

### Cross-Subsystem Operations

```csharp
public class ComplexGameSystem
{
    private readonly IWorldEngineFacade _worldEngine;

    [Inject]
    public ComplexGameSystem(IWorldEngineFacade worldEngine)
    {
        _worldEngine = worldEngine;
    }

    public async Task ProcessGuildTaxCollection(OrganizationId guildId)
    {
        // 1. Get guild details using personas
        var organization = await _worldEngine.Organizations
            .GetOrganizationDetailsAsync(new GetOrganizationDetailsQuery { OrganizationId = guildId });

        // 2. Get all guild members
        var membersQuery = new GetOrganizationMembersQuery { OrganizationId = guildId };
        var members = await _worldEngine.Organizations
            .GetOrganizationMembersAsync(membersQuery);

        foreach (var member in members)
        {
            // 3. Use Personas gateway to get character identity
            var characterIdentity = await _worldEngine.Personas
                .GetCharacterIdentityAsync(member.CharacterId);

            if (characterIdentity == null) continue;

            // 4. Check each member's bank balance
            var balanceQuery = new GetBalanceQuery
            {
                PersonaId = characterIdentity.PersonaId,
                Coinhouse = CoinhouseTag.Parse("cordor_bank")
            };

            var balance = await _worldEngine.Economy.Banking.GetBalanceAsync(balanceQuery);

            if (balance >= 100)
            {
                // 5. Withdraw tax
                var withdrawCommand = WithdrawGoldCommand.Create(
                    characterIdentity.PersonaId,
                    CoinhouseTag.Parse("cordor_bank"),
                    100,
                    "Guild tax");

                await _worldEngine.Economy.Banking.WithdrawGoldAsync(withdrawCommand);

                // 6. Deposit to guild account (using PersonaId from organization)
                var depositCommand = DepositGoldCommand.Create(
                    PersonaId.FromOrganization(guildId),
                    CoinhouseTag.Parse("cordor_bank"),
                    100,
                    $"Tax from {characterIdentity.FullName}");

                await _worldEngine.Economy.Banking.DepositGoldAsync(depositCommand);
            }
        }
    }

    public async Task ProcessCharacterLogin(CharacterId characterId)
    {
        // Use Personas to get player info
        var owner = await _worldEngine.Personas.GetCharacterOwnerAsync(characterId);

        if (owner != null)
        {
            Console.WriteLine($"Player {owner.DisplayName} has {owner.CharacterCount} characters");

            // Get all their characters
            var allCharacters = await _worldEngine.Personas
                .GetPlayerCharactersAsync(owner.CdKey);

            foreach (var char in allCharacters)
            {
                Console.WriteLine($"  - {char.DisplayName}");
            }
        }
    }
}
```

---

## Available Subsystems

### 1. Economy (`_worldEngine.Economy`)
- Bank account management (open, query, eligibility)
- Gold transactions (deposit, withdraw, balance checks)
- **Future**: Shops, storage, taxation

### 2. Organizations (`_worldEngine.Organizations`)
- Organization management (create, disband, update)
- Membership management (add, remove, rank updates)
- Queries (details, members, character organizations)

### 3. Characters (`_worldEngine.Characters`)
- Character registration
- Stats management
- Reputation tracking
- Knowledge and industry contexts

### 4. Industries (`_worldEngine.Industries`)
- Industry queries (all, by tag)
- Crafting operations
- Recipe management
- Membership and learning

### 5. Harvesting (`_worldEngine.Harvesting`)
- Resource node management
- Harvest operations
- Harvest history

### 6. Regions (`_worldEngine.Regions`)
- Region information
- Regional effects

### 7. Traits (`_worldEngine.Traits`)
- Trait definitions
- Character trait management
- Trait effects calculation

### 8. Items (`_worldEngine.Items`)
- Item definitions
- Item properties
- Item categorization

### 9. Codex (`_worldEngine.Codex`)
- Knowledge entries
- Character knowledge tracking
- Lore management

---

## Implementation Status

| Subsystem | Status | Notes |
|-----------|--------|-------|
| Economy | ✅ Fully Implemented | All banking operations working |
| Organizations | 🟡 Partially Implemented | Core queries working, some commands pending |
| Characters | ✅ Fully Implemented | Registration, stats, reputation working |
| Industries | 🟡 Partially Implemented | Core features working, recipes pending |
| Harvesting | 🔴 Stub Only | Needs implementation |
| Regions | 🔴 Stub Only | Needs implementation |
| Traits | 🔴 Stub Only | Needs implementation |
| Items | 🔴 Stub Only | Needs implementation |
| Codex | 🔴 Stub Only | Needs implementation |

---

## Benefits

1. **Simplified Dependencies**: Inject one facade instead of dozens of handlers
2. **Organized Access**: Subsystems are logically grouped (Economy, Organizations, etc.)
3. **Discoverability**: IntelliSense shows all available operations by subsystem
4. **Maintainability**: Changes to handlers don't affect consumer code
5. **Testing**: Easy to mock the entire facade or individual subsystems
6. **Documentation**: Clear structure makes it obvious what's available

---

## Testing with the Facade

```csharp
[Test]
public async Task TestBankingOperations()
{
    // Arrange
    var mockEconomy = new Mock<IEconomySubsystem>();
    mockEconomy
        .Setup(e => e.DepositGoldAsync(It.IsAny<DepositGoldCommand>(), default))
        .ReturnsAsync(CommandResult.Ok());

    var mockFacade = new Mock<IWorldEngineFacade>();
    mockFacade.Setup(f => f.Economy).Returns(mockEconomy.Object);

    var service = new BankManager(mockFacade.Object);

    // Act
    await service.ProcessDeposit(personaId, 100);

    // Assert
    mockEconomy.Verify(e => e.DepositGoldAsync(
        It.Is<DepositGoldCommand>(c => c.Amount.Value == 100),
        default),
        Times.Once);
}
```

---

## Migration Guide

To migrate existing code to use the facade:

1. **Replace individual handler injections** with `IWorldEngineFacade`
2. **Update method calls** to use subsystem accessors (e.g., `_worldEngine.Economy.DepositGoldAsync()`)
3. **Remove `HandleAsync` wrapper** - subsystem methods already handle this

### Before
```csharp
await _depositHandler.HandleAsync(command);
```

### After
```csharp
await _worldEngine.Economy.DepositGoldAsync(command);
```

---

## Future Enhancements

1. **Event Subscriptions**: Subscribe to events through the facade
2. **Bulk Operations**: Batch operations for performance
3. **Caching Layer**: Optional caching for frequently accessed data
4. **Validation**: Centralized validation before command execution
5. **Metrics/Logging**: Built-in operation tracking and logging

---

## See Also

- [WorldEngine Architecture](./README.md)
- [CQRS Pattern](./ARCHITECTURE.md)
- [Command/Query Handlers](./SharedKernel/)
