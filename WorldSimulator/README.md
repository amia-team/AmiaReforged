# WorldSimulator

A standalone .NET 8 Worker Service that executes computationally intensive simulation workflows for the Amia game world.

## Architecture

**WorldSimulator is completely independent** from the game server (AmiaReforged.PwEngine):

- ✅ Separate database instance (PostgreSQL)
- ✅ Independent deployment (Docker container)
- ✅ Loose coupling via HTTP/events (no shared DbContext)
- ✅ Only shares domain models from AmiaReforged.Core
- ✅ Can run without the game server (circuit breaker pauses work)

## Dependencies

- **AmiaReforged.Core**: Shared domain models only (no game server dependencies)
- **PostgreSQL**: Dedicated database instance for simulation state
- **Discord Webhooks**: Optional event notifications (toggle at runtime)

## Responsibilities

1. **Dominion Turn Processing**: Execute government turn calculations off the game server
2. **Settlement Analytics**: Aggregate civic statistics (loyalty, security, prosperity, etc.)
3. **Persona Influence**: Manage influence ledgers and resolve persona actions
4. **Economic Simulation**: Demand/supply models, pricing adjustments, trade routing
5. **Work Queue Management**: Channel-based work queue with circuit breaker pattern

## Configuration

Configuration is managed via:
1. `appsettings.json` - Base configuration
2. `appsettings.{Environment}.json` - Environment-specific overrides
3. `.env` file - Local development (copy from `.env.example`)
4. Environment variables - Docker/Kubernetes deployments

### Key Settings

```bash
# Database - Separate instance from game server
ConnectionStrings__WorldSimulator=Host=localhost;Database=WorldSimulator;Username=worldsim;Password=worldsim

# Circuit Breaker - Pauses work when game server is unavailable
WorldEngine__Host=http://localhost:8080
CircuitBreaker__CheckIntervalSeconds=30

# Simulation
Simulation__MaxConcurrentJobs=5
Simulation__JobTimeoutMinutes=5

# Discord Notifications (optional)
Discord__Enabled=false
Discord__WebhookUrl=https://discord.com/api/webhooks/...
```

## Running Locally

### Prerequisites

- .NET 8 SDK
- PostgreSQL 13+ (separate instance for WorldSimulator)
- (Optional) Game server running for integration testing

### Setup

1. **Create Database**
   ```bash
   createdb WorldSimulator
   createuser worldsim -P  # Set password: worldsim
   psql -d WorldSimulator -c "GRANT ALL PRIVILEGES ON DATABASE WorldSimulator TO worldsim;"
   ```

2. **Configure Environment**
   ```bash
   cp .env.example .env
   # Edit .env with your settings
   ```

3. **Run Migrations**
   ```bash
   dotnet ef database update --project WorldSimulator
   ```

4. **Start Service**
   ```bash
   cd WorldSimulator
   dotnet run
   ```

## Running with Docker

```bash
# Build image
docker build -t worldsimulator:latest -f WorldSimulator/Dockerfile .

# Run container
docker run -d \
  --name worldsimulator \
  -e ConnectionStrings__WorldSimulator="Host=postgres;Database=WorldSimulator;Username=worldsim;Password=worldsim" \
  -e WorldEngine__Host="http://gameserver:8080" \
  -e Discord__Enabled="true" \
  -e Discord__WebhookUrl="https://discord.com/api/webhooks/..." \
  worldsimulator:latest
```

## Database Schema

The WorldSimulator uses its own `simulation` schema with the following tables:

- **SimulationWorkItem**: Work queue entries with status tracking
- **DominionTurnJob**: Dominion turn metadata and results
- **SimulationOutbox/Inbox**: (Future) Reliable messaging patterns

## Circuit Breaker

The service includes a circuit breaker that:

1. Periodically pings the game server health endpoint
2. Opens the circuit (pauses work) when server is unavailable
3. Prevents runaway job accumulation
4. Automatically resumes when server recovers

States:
- **Closed**: Normal operation, work is processed
- **Open**: Server unavailable, work is paused
- **HalfOpen**: Testing if server recovered

## Testing

```bash
# Run all tests
dotnet test WorldSimulator.Tests/

# Run with coverage
dotnet test WorldSimulator.Tests/ --collect:"XPlat Code Coverage"

# Run specific BDD feature
dotnet test --filter "FullyQualifiedName~WorkQueueProcessing"
```

## Discord Notifications

Event notifications can be sent to Discord webhooks:

- **Toggleable at runtime** via `Discord__Enabled` setting
- Supports different severity levels (Info, Warning, Error, Critical)
- Includes rich formatting with embedded fields
- Used for operational monitoring and alerting

## Project Structure

```
WorldSimulator/
├── Application/          # Application layer (workers, processors)
│   ├── SimulationWorker.cs
│   ├── Processors/      # Domain turn processors
│   └── Factories/       # Factory patterns
├── Domain/              # Domain layer (pure business logic)
│   ├── Aggregates/      # Domain aggregates (WorkItem, DominionTurnJob)
│   ├── Events/          # Domain events
│   ├── Services/        # Domain service interfaces
│   ├── ValueObjects/    # Value objects (Status, CircuitState, etc.)
│   └── WorkPayloads/    # Strongly-typed work payloads
├── Infrastructure/      # Infrastructure layer
│   ├── DependencyInjection/
│   ├── Persistence/     # EF Core DbContext and configurations
│   └── Services/        # Infrastructure services (Discord, CircuitBreaker)
└── Program.cs           # Entry point
```

## Development Guidelines

### Domain-Driven Design

- **Aggregates**: Self-contained entities with clear boundaries
- **Value Objects**: Immutable records for typed data (no primitives!)
- **Domain Events**: Capture business events as first-class objects
- **No Primitive Obsession**: Use typed records instead of strings/ints

### Testing

- **BDD with Reqnroll**: Behavior-focused feature specifications
- **Test Behavior, Not Implementation**: Focus on what, not how
- **FluentAssertions**: Readable test assertions
- **Test Containers**: Real PostgreSQL for integration tests

### Code Quality

- Nullable reference types enabled
- Structured logging with Serilog
- Async/await throughout
- Dependency injection via LightInject
- Clean separation of concerns (layered architecture)

## Monitoring

Key metrics to monitor:

- Work queue depth (channel occupancy)
- Job processing time
- Job success/failure rates
- Circuit breaker state changes
- Database connection pool utilization
- Discord notification delivery

## Troubleshooting

### "Failed to connect to database"

- Verify connection string in configuration
- Ensure PostgreSQL is running
- Check database credentials
- Verify network connectivity to database host

### "Circuit breaker open"

- Check game server is running and healthy
- Verify `WorldEngine__Host` configuration
- Review circuit breaker logs for errors
- Work will resume automatically when server recovers

### "No work being processed"

- Check Discord notifications (if enabled) for errors
- Verify database migrations are applied
- Review SimulationWorker logs
- Ensure `Simulation__MaxConcurrentJobs` > 0

## Contributing

See main solution CONTRIBUTING.md for guidelines.

## License

See LICENSE file in solution root.

