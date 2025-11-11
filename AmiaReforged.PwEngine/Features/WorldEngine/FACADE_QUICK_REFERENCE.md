# WorldEngine Facade - Quick Reference

## Setup

```csharp
[ServiceBinding(typeof(MyService))]
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

## Economy Operations

```csharp
// Open account
await _worldEngine.Economy.OpenCoinhouseAccountAsync(command);

// Deposit gold
await _worldEngine.Economy.DepositGoldAsync(
    DepositGoldCommand.Create(personaId, coinhouse, amount, reason));

// Withdraw gold
await _worldEngine.Economy.WithdrawGoldAsync(
    WithdrawGoldCommand.Create(personaId, coinhouse, amount, reason));

// Check balance
int balance = await _worldEngine.Economy.GetBalanceAsync(query);

// Get account details
var account = await _worldEngine.Economy.GetCoinhouseAccountAsync(query);

// Check eligibility
var eligibility = await _worldEngine.Economy.GetCoinhouseAccountEligibilityAsync(query);
```

## Organization Operations

```csharp
// Create organization
await _worldEngine.Organizations.CreateOrganizationAsync(command);

// Get organization details
var org = await _worldEngine.Organizations.GetOrganizationDetailsAsync(query);

// Get character's organizations
var orgs = await _worldEngine.Organizations.GetCharacterOrganizationsAsync(query);

// Get organization members
var members = await _worldEngine.Organizations.GetOrganizationMembersAsync(query);
```

## Character Operations

```csharp
// Register character
await _worldEngine.Characters.RegisterCharacterAsync(characterId);

// Get character
var character = await _worldEngine.Characters.GetCharacterAsync(characterId);

// Get/adjust reputation
int rep = await _worldEngine.Characters.GetReputationAsync(characterId, orgId);
await _worldEngine.Characters.AdjustReputationAsync(characterId, orgId, amount, reason);

// Get contexts
var knowledge = _worldEngine.Characters.GetKnowledgeContext(characterId);
var industry = _worldEngine.Characters.GetIndustryContext(characterId);
```

## Industry Operations

```csharp
// Get industry
var industry = await _worldEngine.Industries.GetIndustryAsync(industryTag);

// Get all industries
var industries = await _worldEngine.Industries.GetAllIndustriesAsync();

// Craft item
await _worldEngine.Industries.CraftItemAsync(command);

// Get character's industries
var memberships = await _worldEngine.Industries.GetCharacterIndustriesAsync(characterId);
```

## Harvesting Operations

```csharp
// Harvest resource
var result = await _worldEngine.Harvesting.HarvestResourceAsync(command);

// Get harvest history
var history = await _worldEngine.Harvesting.GetHarvestHistoryAsync(characterId);

// Check if can harvest
bool canHarvest = await _worldEngine.Harvesting.CanHarvestAsync(characterId, nodeId);
```

## Region Operations

```csharp
// Get region
var region = await _worldEngine.Regions.GetRegionAsync(regionTag);

// Apply regional effect
await _worldEngine.Regions.ApplyRegionalEffectAsync(regionTag, effectId);

// Get regional effects
var effects = await _worldEngine.Regions.GetRegionalEffectsAsync(regionTag);
```

## Trait Operations

```csharp
// Grant trait
await _worldEngine.Traits.GrantTraitAsync(characterId, traitTag);

// Check trait
bool hasTrait = await _worldEngine.Traits.HasTraitAsync(characterId, traitTag);

// Get character traits
var traits = await _worldEngine.Traits.GetCharacterTraitsAsync(characterId);

// Calculate effects
var effects = await _worldEngine.Traits.CalculateTraitEffectsAsync(characterId);
```

## Item Operations

```csharp
// Get item definition
var item = await _worldEngine.Items.GetItemDefinitionAsync(resref);

// Search items
var items = await _worldEngine.Items.SearchItemDefinitionsAsync(searchTerm);

// Get by category
var weapons = await _worldEngine.Items.GetItemsByCategoryAsync(ItemCategory.Weapon);
```

## Codex Operations

```csharp
// Get knowledge entry
var entry = await _worldEngine.Codex.GetKnowledgeEntryAsync(entryId);

// Search knowledge
var entries = await _worldEngine.Codex.SearchKnowledgeAsync(searchTerm);

// Grant knowledge
await _worldEngine.Codex.GrantKnowledgeAsync(characterId, entryId);

// Check knowledge
bool knows = await _worldEngine.Codex.HasKnowledgeAsync(characterId, entryId);
```

## Common Patterns

### Check-Then-Act
```csharp
var balance = await _worldEngine.Economy.GetBalanceAsync(query);
if (balance >= 100)
{
    await _worldEngine.Economy.WithdrawGoldAsync(command);
}
```

### Handle Command Results
```csharp
var result = await _worldEngine.Economy.DepositGoldAsync(command);
if (result.Success)
{
    // Success
    var data = result.Data;
}
else
{
    // Failed
    Console.WriteLine(result.ErrorMessage);
}
```

### Cross-Subsystem Operations
```csharp
// Get character's organizations
var orgs = await _worldEngine.Organizations.GetCharacterOrganizationsAsync(query);

// For each org, check their bank balance
foreach (var org in orgs)
{
    var balance = await _worldEngine.Economy.GetBalanceAsync(
        new GetBalanceQuery
        {
            PersonaId = PersonaId.FromOrganization(org.OrganizationId),
            Coinhouse = coinhouse
        });
}
```

