# WorldSimulator

A dedicated background service for executing computationally intensive simulation workflows for the AmiaReforged persistent world. This service processes dominion turns, settlement analytics, persona actions, and market pricing off-process to keep the Neverwinter Nights server responsive.

## Architecture

WorldSimulator follows **Domain-Driven Design (DDD)** principles with a clear separation of concerns:

```
WorldSimulator/
├── Domain/                    # Pure domain logic, no dependencies
│   ├── Aggregates/           # Aggregate roots (SimulationWorkItem)
│   ├── ValueObjects/         # Value objects (WorkItemStatus, CircuitState)
│   ├── Events/               # Domain events
│   └── Services/             # Domain service interfaces
├── Application/              # Application services and workflows
│   └── SimulationWorker.cs  # Main background worker
├── Infrastructure/           # External concerns (DB, Discord, etc.)
│   ├── Persistence/         # Database context and repositories
│   └── Services/            # Infrastructure services
└── Program.cs               # Entry point and DI container setup
```

### Key Design Decisions

1. **Single Instance per Environment**: No distributed locking needed; simple database-backed queue
2. **Circuit Breaker Pattern**: Automatically pauses work when WorldEngine is unavailable
3. **Shared Database**: Reads from WorldEngine tables, writes only to `simulation` schema
4. **Event-Driven Notifications**: Discord webhook integration for real-time monitoring
5. **No Heavy Dependencies**: No RabbitMQ or MediatR - simple, focused design

## Development Setup

### Prerequisites

- .NET 8 SDK
- PostgreSQL 13+
- Docker (optional, for containerized deployment)

### Getting Started

1. **Clone and navigate to the project:**
   ```bash
   cd /home/zoltan/RiderProjects/AmiaReforged/WorldSimulator
   ```

2. **Configure environment:**
   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

3. **Update database connection in appsettings.json or .env:**
   ```json
   "ConnectionStrings": {
     "PwEngine": "Host=localhost;Database=PwEngine;Username=postgres;Password=yourpassword"
   }
   ```

4. **Run the service:**
   ```bash
   dotnet run
   ```

### Running Tests

We follow **Test-Driven Development (TDD)** and **Behavior-Driven Development (BDD)** practices:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test category
dotnet test --filter "Category=Unit"
```

## Configuration

Configuration can be provided via `appsettings.json`, environment variables, or `.env` file:

| Setting | Description | Default |
|---------|-------------|---------|
| `ConnectionStrings:PwEngine` | PostgreSQL connection string | Required |
| `WorldEngine:Host` | WorldEngine base URL for health checks | `http://localhost:8080` |
| `CircuitBreaker:CheckIntervalSeconds` | How often to check WorldEngine health | `30` |
| `Simulation:PollIntervalSeconds` | How often to poll for new work | `5` |
| `Discord:Enabled` | Enable/disable Discord notifications | `false` |
| `Discord:WebhookUrl` | Discord webhook URL | - |
| `ENVIRONMENT_NAME` | Environment name for logging | `development` |

## Circuit Breaker

The circuit breaker monitors WorldEngine availability:

- **Closed**: Normal operation, work items are processed
- **Open**: WorldEngine unavailable, work processing paused
- **Health Check**: Periodic HTTP ping to configured endpoint

When the circuit opens, the service:
1. Stops polling for new work
2. Logs the failure
3. Publishes a critical event to Discord (if enabled)
4. Automatically retries until WorldEngine is available

## Work Queue

Work items are stored in the database with the following lifecycle:

```
Pending → Processing → Completed
                  ↓
                Failed → [Retry if eligible]
```

Work types supported:
- `DominionTurn` - Territory/region/settlement governance
- `CivicStats` - Settlement statistics aggregation
- `PersonaAction` - Influence system and persona actions
- `MarketPricing` - Economic demand/supply calculations

## Discord Notifications

Enable real-time notifications to Discord:

1. Create a Discord webhook in your server
2. Set `Discord:Enabled=true` in configuration
3. Set `Discord:WebhookUrl` to your webhook URL

Events published:
- Service start/stop
- Circuit breaker state changes
- Work item completion/failure
- Critical errors

## Docker Deployment

Build and run with Docker:

```bash
# Build image
docker build -t worldsimulator:latest .

# Run container
docker run -d \
  --name worldsimulator \
  -e ConnectionStrings__PwEngine="Host=postgres;..." \
  -e Discord__Enabled=true \
  -e Discord__WebhookUrl="https://discord.com/api/webhooks/..." \
  worldsimulator:latest
```

## Development Workflow

We follow DDD and TDD principles:

1. **Write a failing test** (BDD feature or unit test)
2. **Implement domain logic** (pure domain, no infrastructure)
3. **Wire up infrastructure** (database, services)
4. **Verify integration** (test with real dependencies)
5. **Refactor** (maintain clean architecture)

### Example: Adding a New Work Type

1. Create BDD specification:
   ```gherkin
   Feature: New Work Type Processing
     Scenario: Process new work type successfully
       Given a work item of type "NewWorkType" is queued
       When the simulation worker polls for work
       Then the work should be processed correctly
   ```

2. Add test case:
   ```csharp
   [Test]
   public void ProcessNewWorkType_ShouldSucceed() { }
   ```

3. Implement handler in `SimulationWorker.cs`

4. Add integration test with database

## Project Structure

```
WorldSimulator/
├── Domain/                     # Core domain (no dependencies)
├── Application/                # Use cases and workflows
├── Infrastructure/             # External systems integration
├── appsettings.json           # Configuration
├── Program.cs                 # Entry point
└── Dockerfile                 # Container definition

WorldSimulator.Tests/
├── Domain/                     # Domain unit tests
├── Features/                   # BDD specifications (.feature files)
├── Steps/                      # BDD step definitions
└── Integration/               # Integration tests
```

## Contributing

1. All changes must have tests (TDD)
2. Follow DDD principles - keep domain pure
3. Update BDD specs for new features
4. Maintain backward compatibility with WorldEngine
5. Document configuration changes

## License

See LICENSE file in repository root.

