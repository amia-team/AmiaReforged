# Agent Instructions - PwEngine WorldEngine

**Project**: AmiaReforged.PwEngine
**Target**: Real-time game engine for Neverwinter Nights (Anvil .NET 8)
**Last Updated**: October 30, 2025

---

## Project Overview

**PwEngine WorldEngine** is the real-time game engine embedded in the Neverwinter Nights server via Anvil. It handles all synchronous player actions, NWN game state management, and provides REST APIs for the WorldSimulator service.

### Key Characteristics
- **Real-time**: <100ms response time for player actions
- **Synchronous**: Direct NWN script execution
- **Transactional**: ACID guarantees via EF Core + PostgreSQL
- **Event-driven**: Publishes domain events to WorldSimulator
- **REST API Server**: Exposes endpoints for turn-based processing

---

## System Boundaries

### PwEngine Owns (Real-Time)
✅ Player actions (immediate response needed)
✅ NWN game state (inventory, XP, creature spawning)
✅ Transactional integrity (banking, trading, property transfers)
✅ Command/Query handling via MediatR
✅ Event publishing to WorldSimulator
✅ REST API endpoints for cross-service communication

### PwEngine Does NOT Own (Deferred to WorldSimulator)
❌ Turn-based processing (dominion turns, civic stats)
❌ Heavy analytics (demand/supply simulation, AI decisions)
❌ Scheduled batch jobs (interest calculation, foreclosures)
❌ Long-running computations (>100ms)

**See**: [SYSTEM_BOUNDARIES.md](../../SYSTEM_BOUNDARIES.md) for full delineation

---

## Architecture Principles

### 1. Parse, Don't Validate
**Always use strongly-typed value objects:**
```csharp
// ❌ BAD - Stringly typed, error-prone
public void Transfer(string fromId, string toId, int amount) { }

// ✅ GOOD - Compilation-safe, self-validating
public void Transfer(TreasuryId fromId, TreasuryId toId, GoldAmount amount) { }
```

**Value objects validate at construction:**
```csharp
public readonly record struct GoldAmount
{
    public int Value { get; }

    public GoldAmount(int value)
    {
        if (value < 0)
            throw new ArgumentException("Gold amount cannot be negative");
        Value = value;
    }
}
```

### 2. Domain-Driven Design (DDD)
**Organize by feature, not layer:**
```
Features/WorldEngine/
  Economy/
    Banking/
      Domain/         # Aggregates, value objects, events
      Application/    # Commands, handlers, queries
      Infrastructure/ # Persistence, API controllers
      Tests/          # BDD scenarios, unit tests
```

**Aggregates enforce invariants:**
```csharp
public class Treasury // Aggregate root
{
    public void Withdraw(GoldAmount amount)
    {
        if (amount.Value > AvailableBalance)
            throw new InsufficientFundsException();

        _balance -= amount;
        _events.Add(new GoldWithdrawnEvent(Id, amount));
    }
}
```

### 3. CQRS (Command Query Responsibility Segregation)
**Commands modify state:**
```csharp
public record WithdrawGoldCommand(TreasuryId TreasuryId, GoldAmount Amount);

public class WithdrawGoldHandler : IRequestHandler<WithdrawGoldCommand>
{
    public async Task Handle(WithdrawGoldCommand cmd, CancellationToken ct)
    {
        var treasury = await _repository.GetAsync(cmd.TreasuryId);
        treasury.Withdraw(cmd.Amount);
        await _repository.SaveAsync(treasury);
    }
}
```

**Queries return data (read-only):**
```csharp
public record GetTreasuryBalanceQuery(TreasuryId TreasuryId);

public class GetTreasuryBalanceHandler : IRequestHandler<GetTreasuryBalanceQuery, TreasuryBalance>
{
    public async Task<TreasuryBalance> Handle(GetTreasuryBalanceQuery query, CancellationToken ct)
    {
        return await _repository.GetBalanceAsync(query.TreasuryId);
    }
}
```

### 4. Event-Driven Integration
**Publish domain events:**
```csharp
public class Treasury
{
    private readonly List<DomainEvent> _events = new();

    public void Withdraw(GoldAmount amount)
    {
        // ... withdraw logic ...
        _events.Add(new GoldWithdrawnEvent(Id, amount, DateTimeOffset.UtcNow));
    }

    public IReadOnlyList<DomainEvent> GetEvents() => _events;
}
```

**Send events to WorldSimulator via webhooks:**
```csharp
public class EventPublisher
{
    public async Task PublishAsync(DomainEvent evt)
    {
        await _httpClient.PostAsJsonAsync(
            $"{_config.WorldSimulatorUrl}/api/simulator/events",
            evt);
    }
}
```

### 5. BDD/TDD Always
**Write scenarios first (Reqnroll):**
```gherkin
Scenario: Withdraw gold from treasury
  Given a treasury exists with 500 gold
  When the player withdraws 200 gold
  Then the treasury balance should be 300 gold
  And the player should have 200 gold in inventory
  And a GoldWithdrawnEvent should be published
```

**Write unit tests for value objects:**
```csharp
[Test]
public void GoldAmount_RejectsNegativeValues()
{
    Assert.Throws<ArgumentException>(() => new GoldAmount(-100));
}
```

---

## Code Style Guidelines

### Naming Conventions
```csharp
// Value objects: Noun + descriptive suffix
public readonly record struct TreasuryId { }
public readonly record struct GoldAmount { }

// Commands: Verb + Noun + "Command"
public record OpenTreasuryCommand(PersonaId Owner, GoldAmount InitialDeposit);

// Events: Noun + Past-tense verb + "Event"
public record TreasuryOpenedEvent(TreasuryId Id, PersonaId Owner, DateTimeOffset Timestamp);

// Handlers: Command/Query name + "Handler"
public class OpenTreasuryHandler : IRequestHandler<OpenTreasuryCommand> { }

// Aggregates: Domain noun (singular)
public class Treasury { }
public class Property { }
```

### File Organization
```
Domain/
  Aggregates/
    Treasury.cs              # One aggregate per file
  ValueObjects/
    TreasuryId.cs           # One value object per file
    GoldAmount.cs
  Events/
    BankingEvents.cs        # Group related events

Application/
  Commands/
    OpenTreasuryCommand.cs  # Command + Handler in same file
  Queries/
    GetTreasuryBalanceQuery.cs

Infrastructure/
  Persistence/
    Entities/
      PersistentTreasury.cs  # EF Core entity
    Configurations/
      TreasuryConfiguration.cs # Fluent API config
  API/
    BankingController.cs    # REST endpoints
```

### Comments
```csharp
// ✅ GOOD - Explains WHY, not WHAT
// Circuit breaker pauses simulation if PwEngine is unhealthy
if (!_circuitBreaker.IsAvailable())
    return;

// ❌ BAD - States the obvious
// Check if circuit breaker is available
if (!_circuitBreaker.IsAvailable())
    return;

// ✅ GOOD - XML docs for public APIs
/// <summary>
/// Withdraws gold from a treasury and transfers it to the persona's inventory.
/// </summary>
/// <exception cref="InsufficientFundsException">Thrown when balance is insufficient</exception>
public void Withdraw(GoldAmount amount) { }
```

---

## REST API Standards

### Controller Structure
```csharp
[ApiController]
[Route("api/worldengine/[controller]")]
public class BankingController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("treasuries/{treasuryId}/balance")]
    [ProducesResponseType(typeof(TreasuryBalance), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TreasuryBalance>> GetBalance(string treasuryId)
    {
        var query = new GetTreasuryBalanceQuery(TreasuryId.Parse(treasuryId));
        var balance = await _mediator.Send(query);
        return Ok(balance);
    }

    [HttpPost("apply-interest")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApplyInterest([FromBody] ApplyInterestRequest request)
    {
        var command = new ApplyInterestCommand(
            TreasuryId.Parse(request.TreasuryId),
            new GoldAmount(request.InterestAmount));

        await _mediator.Send(command);
        return Accepted(new { transactionId = command.CorrelationId });
    }
}
```

### HTTP Status Codes
- `200 OK` - Successful query
- `201 Created` - Resource created (return Location header)
- `202 Accepted` - Command accepted (async processing)
- `400 Bad Request` - Invalid input (return validation errors)
- `404 Not Found` - Resource doesn't exist
- `409 Conflict` - Business rule violation or idempotency conflict
- `500 Internal Server Error` - Unexpected failure
- `503 Service Unavailable` - Circuit breaker open

### Request/Response DTOs
```csharp
// Request DTOs are simple POCOs
public class ApplyInterestRequest
{
    public string TreasuryId { get; set; } = string.Empty;
    public int InterestAmount { get; set; }
    public string? CorrelationId { get; set; }
}

// Response DTOs are records
public record TreasuryBalance(int Available, int Held, int Total);

// Convert to domain types in controller, not in handler
var command = new ApplyInterestCommand(
    TreasuryId.Parse(request.TreasuryId),  // ← Parse here
    new GoldAmount(request.InterestAmount));
```

---

## Database Patterns

### EF Core Entity Configuration
```csharp
public class TreasuryConfiguration : IEntityTypeConfiguration<PersistentTreasury>
{
    public void Configure(EntityTypeBuilder<PersistentTreasury> builder)
    {
        builder.ToTable("treasuries", "worldengine");

        builder.HasKey(t => t.Id);

        // Value object conversion
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TreasuryId(value));

        builder.Property(t => t.AvailableBalance)
            .HasConversion(
                amount => amount.Value,
                value => new GoldAmount(value));

        // Optimistic concurrency
        builder.Property(t => t.Version)
            .IsRowVersion();
    }
}
```

### Repository Pattern
```csharp
public interface ITreasuryRepository
{
    Task<Treasury> GetAsync(TreasuryId id);
    Task SaveAsync(Treasury treasury);
}

public class TreasuryRepository : ITreasuryRepository
{
    public async Task<Treasury> GetAsync(TreasuryId id)
    {
        var entity = await _context.Treasuries
            .Include(t => t.LedgerEntries)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (entity == null)
            throw new TreasuryNotFoundException(id);

        return entity.ToDomain();
    }

    public async Task SaveAsync(Treasury treasury)
    {
        var entity = PersistentTreasury.FromDomain(treasury);
        _context.Update(entity);
        await _context.SaveChangesAsync();

        // Publish events after successful save
        foreach (var evt in treasury.GetEvents())
            await _eventPublisher.PublishAsync(evt);
    }
}
```

### Schema Organization
- **Schema**: `worldengine` (PwEngine owns this schema)
- **Read Access**: WorldSimulator can query (read-only)
- **Write Access**: Only PwEngine writes to this schema

---

## Testing Standards

### BDD Scenarios (Reqnroll)
**Location**: `Features/WorldEngine/{Domain}/Tests/BDD/Features/`

```gherkin
Feature: Treasury Management
  As a persona in the world
  I want to manage my gold through a treasury
  So that I can safely store and transfer wealth

  Scenario: Open a new treasury
    Given a CoinHouse exists in Cordor
    And a persona "Lord Blackwood" with 1000 gold
    When Lord Blackwood opens a treasury with 500 gold deposit
    Then a treasury should be created
    And the treasury balance should be 500 gold
    And Lord Blackwood should have 500 gold remaining
    And a TreasuryOpenedEvent should be published
```

### Step Definitions
```csharp
[Binding]
public class TreasurySteps
{
    [Given(@"a persona ""(.*)"" with (.*) gold")]
    public void GivenAPersonaWithGold(string name, int gold)
    {
        var persona = new Persona(PersonaId.New(), name);
        persona.AddGold(new GoldAmount(gold));
        _scenarioContext["Persona"] = persona;
    }

    [When(@"(.*) opens a treasury with (.*) gold deposit")]
    public async Task WhenPersonaOpensTreasury(string name, int deposit)
    {
        var persona = _scenarioContext.Get<Persona>("Persona");
        var command = new OpenTreasuryCommand(persona.Id, new GoldAmount(deposit));
        var treasury = await _handler.Handle(command, CancellationToken.None);
        _scenarioContext["Treasury"] = treasury;
    }

    [Then(@"the treasury balance should be (.*) gold")]
    public void ThenTreasuryBalanceShouldBe(int expected)
    {
        var treasury = _scenarioContext.Get<Treasury>("Treasury");
        treasury.AvailableBalance.Value.Should().Be(expected);
    }
}
```

### Unit Tests
```csharp
[TestFixture]
public class GoldAmountTests
{
    [Test]
    public void Constructor_WithNegativeValue_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new GoldAmount(-100));
    }

    [Test]
    public void Add_TwoAmounts_ReturnsSum()
    {
        var a = new GoldAmount(100);
        var b = new GoldAmount(50);
        var result = a + b;
        Assert.That(result.Value, Is.EqualTo(150));
    }

    [Test]
    public void Subtract_InsufficientFunds_ThrowsException()
    {
        var a = new GoldAmount(100);
        var b = new GoldAmount(150);
        Assert.Throws<InvalidOperationException>(() => a - b);
    }
}
```

---

## Integration with NWN (Anvil)

### NWScript Bridge Pattern
```csharp
// C# side (PwEngine)
[ScriptHandler("banking_withdraw")]
public class BankingWithdrawScript
{
    [Inject] private IMediator _mediator = null!;

    public async Task Execute(NwPlayer player, int amount)
    {
        var treasuryId = player.GetTreasuryId(); // Extension method
        var command = new WithdrawGoldCommand(treasuryId, new GoldAmount(amount));

        try
        {
            await _mediator.Send(command);
            player.SendServerMessage($"Withdrew {amount} gold from treasury.");
        }
        catch (InsufficientFundsException)
        {
            player.SendServerMessage("Insufficient funds.", Color.Red);
        }
    }
}
```

### Conversation System Integration
```csharp
public class CoinHouseConversation : ConversationScript
{
    protected override void OnConversationStart(NwPlayer speaker)
    {
        var treasury = await _repository.GetByPersonaAsync(speaker.PersonaId);

        AddNode("Welcome to the CoinHouse!");
        AddNode($"Your balance: {treasury.AvailableBalance.Value} gold");
        AddChoice("Deposit gold", OnDeposit);
        AddChoice("Withdraw gold", OnWithdraw);
        AddChoice("View ledger", OnViewLedger);
    }
}
```

---

## Common Patterns & Anti-Patterns

### ✅ DO: Use Nullable Reference Types
```csharp
#nullable enable

public class Treasury
{
    public TreasuryId Id { get; } // Non-nullable
    public string? Notes { get; set; } // Nullable - optional field
}
```

### ✅ DO: Use Records for DTOs
```csharp
public record TreasuryBalance(int Available, int Held, int Total);
public record ApplyInterestCommand(TreasuryId TreasuryId, GoldAmount Amount);
```

### ✅ DO: Use Pattern Matching
```csharp
public decimal CalculateFee(TransactionType type, GoldAmount amount) => type switch
{
    TransactionType.Deposit => 0m,
    TransactionType.Withdrawal => amount.Value * 0.01m,
    TransactionType.Transfer => 5m + (amount.Value * 0.005m),
    _ => throw new ArgumentException($"Unknown transaction type: {type}")
};
```

### ❌ DON'T: Use Magic Strings
```csharp
// ❌ BAD
if (status == "pending") { }

// ✅ GOOD
if (status == AgreementStatus.Pending) { }
```

### ❌ DON'T: Put Business Logic in Controllers
```csharp
// ❌ BAD
[HttpPost("transfer")]
public async Task<IActionResult> Transfer(TransferRequest req)
{
    var from = await _db.Treasuries.FindAsync(req.FromId);
    if (from.Balance < req.Amount) // ← Business logic in controller!
        return BadRequest();
    from.Balance -= req.Amount;
    await _db.SaveChangesAsync();
    return Ok();
}

// ✅ GOOD
[HttpPost("transfer")]
public async Task<IActionResult> Transfer(TransferRequest req)
{
    var command = new TransferGoldCommand(
        TreasuryId.Parse(req.FromId),
        TreasuryId.Parse(req.ToId),
        new GoldAmount(req.Amount));

    await _mediator.Send(command); // ← Handler contains business logic
    return Accepted();
}
```

### ❌ DON'T: Return Domain Objects from APIs
```csharp
// ❌ BAD - Exposes internal structure
[HttpGet("{id}")]
public async Task<Treasury> GetTreasury(string id) { }

// ✅ GOOD - Returns DTO
[HttpGet("{id}")]
public async Task<TreasuryDto> GetTreasury(string id)
{
    var treasury = await _repository.GetAsync(TreasuryId.Parse(id));
    return TreasuryDto.FromDomain(treasury);
}
```

---

## Error Handling

### Domain Exceptions
```csharp
public class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(TreasuryId treasuryId, GoldAmount requested, GoldAmount available)
        : base($"Treasury {treasuryId} has insufficient funds. Requested: {requested.Value}, Available: {available.Value}")
    {
    }
}
```

### Global Exception Handler
```csharp
app.UseExceptionHandler(app => app.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    var (statusCode, message) = exception switch
    {
        DomainException => (StatusCodes.Status400BadRequest, exception.Message),
        NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
        _ => (StatusCodes.Status500InternalServerError, "An error occurred")
    };

    context.Response.StatusCode = statusCode;
    await context.Response.WriteAsJsonAsync(new { error = message });
}));
```

---

## Performance Considerations

### Async/Await Everywhere
```csharp
// ✅ GOOD
public async Task<Treasury> GetAsync(TreasuryId id)
{
    return await _context.Treasuries.FirstOrDefaultAsync(t => t.Id == id);
}

// ❌ BAD - Blocking
public Treasury Get(TreasuryId id)
{
    return _context.Treasuries.FirstOrDefault(t => t.Id == id);
}
```

### Query Optimization
```csharp
// ✅ GOOD - Projects to DTO, minimal data transfer
var balances = await _context.Treasuries
    .Where(t => t.SettlementId == settlementId)
    .Select(t => new TreasuryBalance(t.AvailableBalance, t.HeldBalance, t.TotalBalance))
    .ToListAsync();

// ❌ BAD - Loads entire aggregate
var treasuries = await _context.Treasuries
    .Include(t => t.LedgerEntries) // ← Unnecessary for balance query
    .Where(t => t.SettlementId == settlementId)
    .ToListAsync();
```

### Caching (When Appropriate)
```csharp
public async Task<IReadOnlyList<FeePolicy>> GetFeePoliciesAsync(CoinHouseId id)
{
    var cacheKey = $"fee-policies:{id}";

    return await _cache.GetOrCreateAsync(cacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return await _repository.GetFeePoliciesAsync(id);
    });
}
```

---

## Dependency Injection

### Service Registration
```csharp
// Program.cs or Startup.cs
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddScoped<ITreasuryRepository, TreasuryRepository>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddHttpClient<IWorldSimulatorClient, WorldSimulatorClient>();
```

### Constructor Injection
```csharp
public class OpenTreasuryHandler : IRequestHandler<OpenTreasuryCommand, TreasuryId>
{
    private readonly ITreasuryRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public OpenTreasuryHandler(
        ITreasuryRepository repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }
}
```

---

## Quick Reference

### Decision Tree: Where Does Code Go?

1. **Is it a business rule/invariant?** → Domain (Aggregate)
2. **Is it an operation/workflow?** → Application (Handler)
3. **Is it database-specific?** → Infrastructure (Repository)
4. **Is it an API endpoint?** → Infrastructure (Controller)
5. **Is it a value validation?** → Domain (Value Object)
6. **Is it a data contract?** → Application (DTO)

### File Naming Conventions
- Aggregates: `Treasury.cs`, `Property.cs`
- Value Objects: `TreasuryId.cs`, `GoldAmount.cs`
- Commands: `OpenTreasuryCommand.cs`
- Events: `BankingEvents.cs` (group related events)
- Handlers: `OpenTreasuryHandler.cs`
- Controllers: `BankingController.cs`
- Repositories: `TreasuryRepository.cs`
- EF Entities: `PersistentTreasury.cs`
- EF Configs: `TreasuryConfiguration.cs`

---

## Resources

- [SYSTEM_BOUNDARIES.md](../../SYSTEM_BOUNDARIES.md) - System delineation
- [ADR_REST_OVER_GRPC.md](../../ADR_REST_OVER_GRPC.md) - API decision rationale
- [BANKING_IMPLEMENTATION_PLAN.md](../../BANKING_IMPLEMENTATION_PLAN.md) - Implementation guide
- [EconomyGameDesign.MD](Features/WorldEngine/EconomyGameDesign.MD) - Domain requirements
- [Requirements.md](Features/WorldEngine/Economy/Requirements.md) - Banking requirements

---

## Getting Help

**When stuck, ask:**
1. Is this a real-time action or turn-based? (Determines PwEngine vs WorldSimulator)
2. Where does this business rule belong? (Domain, Application, Infrastructure)
3. What value objects can make this type-safe?
4. What's the BDD scenario for this feature?
5. How will this integrate with NWN/Anvil?

**Red flags:**
- 🚩 String IDs instead of typed IDs
- 🚩 Business logic in controllers
- 🚩 Missing BDD scenarios
- 🚩 Blocking I/O (.Result, .Wait())
- 🚩 God classes (>500 lines)
- 🚩 Magic numbers/strings

---

**Remember**: Parse, don't validate. Use types. Write tests first. Keep it simple. Ship it. 🚀

