# Phase 3: API Migration to Persona

## Date: October 27, 2025

## Status: 🚀 KICKOFF

Phase 3 focuses on migrating existing APIs to use PersonaId, enabling cross-persona operations.

## Objectives

1. **Create Transaction Command/Query Infrastructure**
   - Implement CQRS pattern for transactions
   - Create TransferGold command using PersonaId
   - Add transaction logging with PersonaId

2. **Update Coinhouse APIs**
   - Enable deposits/withdrawals using PersonaId
   - Support organization accounts
   - Track transaction history

3. **Create Reputation System APIs**
   - Reputation between any persona types
   - Query reputation by PersonaId

4. **Update Ownership/Industry APIs**
   - Organizations can own industries
   - Track ownership via PersonaId

## Phase 3 Scope

### Part 1: Transaction Infrastructure (Current)
- [ ] Create ICommand/ICommandHandler infrastructure
- [ ] Create IQuery/IQueryHandler infrastructure
- [ ] Create TransferGoldCommand
- [ ] Create TransferGoldCommandHandler
- [ ] Create Transaction entity with PersonaId
- [ ] Create TransactionRepository
- [ ] Add transaction tests

### Part 2: Coinhouse API Migration
- [ ] Update CoinhouseService to use PersonaId
- [ ] Create DepositGoldCommand
- [ ] Create WithdrawGoldCommand
- [ ] Update CoinHouseAccount to support PersonaId
- [ ] Add coinhouse transaction tests

### Part 3: Reputation System
- [ ] Create Reputation entity with PersonaId
- [ ] Create IReputationRepository
- [ ] Create GrantReputationCommand
- [ ] Create GetReputationQuery
- [ ] Add reputation tests

### Part 4: Ownership/Industry Updates
- [ ] Update Industry ownership to use PersonaId
- [ ] Create AssignOwnershipCommand
- [ ] Update membership tracking
- [ ] Add ownership tests

## Implementation Strategy

### CQRS Pattern
Commands and queries are separated:

**Commands** (write operations):
```csharp
public sealed record TransferGoldCommand(
    PersonaId From,
    PersonaId To,
    Quantity Amount,
    string? Memo = null
) : ICommand;

[ServiceBinding(typeof(ICommandHandler<TransferGoldCommand>))]
public class TransferGoldCommandHandler : ICommandHandler<TransferGoldCommand>
{
    public async Task<CommandResult> HandleAsync(TransferGoldCommand cmd, CancellationToken ct)
    {
        // Validate personas exist
        // Execute transfer
        // Log transaction
        // Return result
    }
}
```

**Queries** (read operations):
```csharp
public sealed record GetTransactionHistoryQuery(
    PersonaId PersonaId,
    int PageSize = 50,
    int Page = 0
) : IQuery<IEnumerable<Transaction>>;

[ServiceBinding(typeof(IQueryHandler<GetTransactionHistoryQuery, IEnumerable<Transaction>>))]
public class GetTransactionHistoryQueryHandler : IQueryHandler<GetTransactionHistoryQuery, IEnumerable<Transaction>>
{
    public async Task<IEnumerable<Transaction>> HandleAsync(GetTransactionHistoryQuery query, CancellationToken ct)
    {
        // Query transactions for persona
        // Return results
    }
}
```

## Benefits

### Cross-Persona Transactions
```csharp
// Player → Guild
await handler.HandleAsync(new TransferGoldCommand(
    playerPersona.Id,
    guildPersona.Id,
    Quantity.Parse(100)
));

// Guild → Bank
await handler.HandleAsync(new TransferGoldCommand(
    guildPersona.Id,
    bankPersona.Id,
    Quantity.Parse(5000)
));

// System → Player (rewards)
await handler.HandleAsync(new TransferGoldCommand(
    SystemPersona.Create("Rewards").Id,
    playerPersona.Id,
    Quantity.Parse(50)
));
```

### Unified Transaction History
```csharp
// Get all transactions for any persona
var transactions = await queryHandler.HandleAsync(
    new GetTransactionHistoryQuery(guildPersona.Id)
);

// Works for characters, organizations, coinhouses, etc.
```

## Testing Strategy

1. **Command Tests** - Test each command's business logic
2. **Handler Tests** - Test command/query handlers
3. **Integration Tests** - Test actual database operations
4. **Scenario Tests** - Test cross-persona workflows

## Migration Path

### Backward Compatibility
Keep existing CharacterId-based methods temporarily:

```csharp
// Old method (deprecated)
[Obsolete("Use TransferGoldCommand with PersonaId instead")]
public void TransferGold(CharacterId from, CharacterId to, int amount)
{
    // Convert to PersonaId and delegate
    var cmd = new TransferGoldCommand(
        from.ToPersonaId(),
        to.ToPersonaId(),
        Quantity.Parse(amount)
    );
    _commandHandler.HandleAsync(cmd).Wait();
}

// New method
public async Task<CommandResult> TransferGold(
    PersonaId from,
    PersonaId to,
    Quantity amount)
{
    return await _commandHandler.HandleAsync(
        new TransferGoldCommand(from, to, amount)
    );
}
```

## Success Criteria

- [ ] CQRS infrastructure in place
- [ ] TransferGold works for all persona types
- [ ] Transaction logging includes PersonaId
- [ ] 100% test coverage on new commands/queries
- [ ] Documentation updated
- [ ] Existing functionality maintained

## Timeline

**Part 1 (Transaction Infrastructure):** Current focus
**Part 2 (Coinhouse APIs):** After Part 1 complete
**Part 3 (Reputation System):** After Part 2 complete
**Part 4 (Ownership):** After Part 3 complete

---

**Phase 3 Status: 🚀 KICKED OFF - Part 1 In Progress**

Starting with CQRS infrastructure and TransferGold command implementation.

