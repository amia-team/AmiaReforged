# Phase 5: Public API Layer

**Status**: ⏳ Not Started
**Planned Start**: Week 8

---

## Goal

Provide a simple, discoverable API for other developers to interact with WorldEngine. Abstract commands, queries, and events behind a unified `IWorldEngine` façade.

---

## Façade Design

```csharp
[ServiceBinding(typeof(IWorldEngine))]
public interface IWorldEngine
{
    // Commands
    Task<CommandResult> ExecuteAsync<TCommand>(
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand;

    // Queries
    Task<TResult> QueryAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken ct = default)
        where TQuery : IQuery<TResult>;

    // Events
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IDomainEvent;
}
```

---

## Example Usage

```csharp
public class PlayerGoldTransferService
{
    private readonly IWorldEngine _world;

    public async Task TransferGold(NwPlayer from, NwPlayer to, int amount)
    {
        var cmd = new TransferGoldCommand(
            From: PersonaId.FromCharacter(from.Id),
            To: PersonaId.FromCharacter(to.Id),
            Amount: Quantity.Parse(amount),
            Reason: "Player-initiated transfer"
        );

        var result = await _world.ExecuteAsync(cmd);

        if (!result.Success)
        {
            from.SendServerMessage($"Transfer failed: {result.ErrorMessage}");
        }
    }
}
```

---

## Lock Down Internals

Once façade is complete:
- Mark repositories as `internal` where possible
- Mark domain services as `internal`
- External code must use `IWorldEngine`
- Compilation enforces boundaries

---

## Documentation

Create comprehensive API guide:
- Command/query catalog
- Event catalog
- Usage examples
- Migration guide

---

**Previous Phase**: [Phase 4: Event Bus](PHASE4_EVENT_BUS.md)
# Phase 3.4: Other Subsystems

**Status**: ⏳ Not Started
**Planned Start**: After Phase 3.3 completion

---

## Goal

Apply CQRS pattern to remaining WorldEngine subsystems: Industries, Organizations, Harvesting, Regions, and Traits.

---

## Subsystems to Refactor

### Industries
- Commands: StartProduction, CancelProduction, ClaimOutput
- Queries: GetActiveProduction, GetIndustryDefinition, GetMemberIndustries

### Organizations
- Commands: GrantReputation, RevokeReputation, UpdateRoster
- Queries: GetOrganizationReputation, GetMemberOrganizations

### Harvesting
- Commands: HarvestNode, DepletNode, RegenerateNode
- Queries: GetAvailableNodes, GetNodeStatus

### Regions
- Commands: UpdateSettlementPolicy, SetRegionTaxRate
- Queries: GetSettlementsInRegion, GetRegionPolicy

### Traits
- Commands: GrantTrait, RevokeTrait
- Queries: GetPersonaTraits, GetTraitDefinition

---

## Approach

Follow the pattern established in Phase 3.2 and 3.3:
1. Write BDD tests first
2. Implement commands with factory validation
3. Implement queries with read-only semantics
4. Publish events from handlers
5. Integration tests for workflows

---

**Previous Phase**: [Phase 3.3: Economy Expansion](PHASE3_3_ECONOMY_EXPANSION.md)
**Next Phase**: [Phase 4: Event Bus](PHASE4_EVENT_BUS.md)

