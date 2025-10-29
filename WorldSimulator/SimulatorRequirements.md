# Simulation Service Requirements

**Document Owners**: WorldEngine Team
**Last Updated**: October 29, 2025

---

## 1. Purpose & Scope

The Simulation Service executes computationally intensive workflows (dominion turns, settlement analytics, persona action resolution) off-process so the Neverwinter Nights server remains responsive. This document captures functional and non-functional requirements for the new project that will run in a dedicated container within the same solution.

### In Scope

- Dominion turn orchestration for governments and subordinate entities
- Aggregation of settlement civic statistics (loyalty, security, manpower, prosperity, happiness, military might, arcane power, defense)
- Persona influence ledger management and persona action resolution (intrigue, diplomacy, others)
- Support for demand/supply analytics, import/export scheduling, and market repricing heuristics
- Event bus integration with WorldEngine via the simulation portal
- Shared persistence model leveraging existing PostgreSQL schema with optimistic concurrency

### Out of Scope

- Player-facing UI (handled by DM/Admin dashboards and NWScript tooling)
- Direct NWN game script execution
- Long-term telemetry warehousing (handled elsewhere)

---

- **Process Isolation**: Runs as a separate .NET project/container, completely independent from the game server.
- **Deployment**: Dockerized workload deployed alongside NWN server containers within the same network. Supports horizontal scaling when required.
- **Technology Stack**: .NET 8 Worker Service, `System.Threading.Channels` for in-process work queues, PostgreSQL via EF Core DbContext (separate database instance), Serilog for structured logging, Polly for resilience patterns, Discord webhooks for event notifications.
- **Communication**: HTTP/gRPC endpoints for WorldEngine integration, Discord webhooks for notifications and event logging (toggleable at runtime).
- **Contracts**: Defines its own domain value objects, commands, and events. Communication with WorldEngine happens via agreed-upon DTOs/messages to ensure loose coupling.

---

## 3. Domain Responsibilities

1. **Dominion Turn Execution**
   - Consume `RunDominionTurnCommand`
   - Create and manage `DominionTurnJob` aggregates (Queued → Running → Completed/Failed)
   - Iterate hierarchy: Territory → Region → Settlement → Organizations → Markets
   - Publish outcome events (`DominionTurnCompletedEvent`, `DominionTurnJobFailedEvent`)

2. **Settlement Civic Stat Aggregation**
   - Ingest recent economic, crime, population, and satisfaction events
   - Produce `SettlementCivicStats` snapshots (`LoyaltyScore`, `SecurityScore`, `ManpowerLevel`, `ProsperityScore`, `HappinessIndex`, `MilitaryMight`, `ArcanePower`, `DefenseRating`)
   - Publish `SettlementCivicStatsUpdatedEvent`

3. **Persona Influence System**
   - Maintain `PersonaInfluenceLedger` balances (earn/spend)
   - Validate and queue persona actions (`PersonaActionQueuedEvent`)
   - Resolve actions (intrigue, diplomacy, etc.), emitting `PersonaActionResolvedEvent`

4. **Economic Analytics**
   - Run demand/supply models for import/export routing and pricing adjustments
   - Feed `RepriceMarketInventoryCommand` responses and `MarketPricesAdjustedEvent`

5. **Projection Synchronization**
   - Subscribe to projection refresh notifications (e.g., `EconomicCenterView`, `SettlementCivicStatsView`, `InfluenceLedgerView`)
   - Cache snapshots locally with version stamps for deterministic simulations

---

## 4. Interfaces

### 4.1 Commands Consumed

| Command | Purpose |
| --- | --- |
| `RunDominionTurnCommand` | Start dominion turn job for specified government |
| `UpdateSettlementDemandCommand` | Provide demand snapshot as simulation input |
| `QueuePersonaActionCommand` | Submit persona action request for processing |
| `GrantInfluenceCommand` | Increase persona influence balance (idempotent) |
| `SpendInfluenceCommand` | Attempt to spend influence; validated in service |
| `RepriceMarketInventoryCommand` | Compute pricing multipliers from demand signals |
| `UpdateSettlementCivicStatsCommand` | (Optional) Manual overrides or external adjustments |

### 4.2 Events Published

- `DominionTurnJobQueuedEvent`
- `DominionTurnJobStartedEvent`
- `DominionTurnCompletedEvent`
- `DominionTurnJobFailedEvent`
- `SettlementCivicStatsUpdatedEvent`
- `InfluenceGrantedEvent`
- `InfluenceSpentEvent`
- `PersonaActionQueuedEvent`
- `PersonaActionResolvedEvent`
- `MarketPricesAdjustedEvent`

### 4.3 Events Consumed

- Economic/industry events (`ResourceHarvestedEvent`, `ProductionRecordedEvent`, etc.)
- Crime and reputation events impacting loyalty and happiness
- Organization membership changes affecting manpower/manpower distribution
- Projection invalidation notifications from WorldEngine

---

## 5. Data & Persistence Requirements

- **Operates with a completely separate PostgreSQL database instance** from WorldEngine and the game server.
- The WorldSimulator is **fully independent** and does not share DbContext, schemas, or database instances with AmiaReforged.Core or AmiaReforged.PwEngine.
- Simulation Service persists **only** orchestration metadata and simulation state it owns:
  - `SimulationWorkItem` (work queue entries, status, payloads)
  - `DominionTurnJob` (dominion turn metadata, status, timings, results)
  - `SimulationOutbox` / `SimulationInbox` (optional) for reliable messaging and retries
  - Future: simulation snapshots, cached projections, event log
- **Communication with WorldEngine is exclusively through events/commands** (no direct database access).
- WorldEngine remains the authoritative source for all game domain data. The simulator requests data via commands/queries and receives responses via events.
- Employ optimistic concurrency tokens (rowversion/`xmin`) on simulation-owned tables to avoid double processing when multiple instances run.
- Database connection configured via environment variables, completely separate from game server database credentials.

---

## 6. Configuration & Definitions

- `SimulationServiceSettings.json` (docker mounted or config map) containing:
  - Event bus connection (RabbitMQ, Redis, etc.)
  - Database connection string with credentials/SSL
  - Dominion turn cadence overrides
  - Maximum concurrent jobs, retry/backoff policies
- Reuse WorldEngine file-based definitions via mounted volume or API:
  - `SettlementCivicStatDefinition` (weights, decay curves, thresholds)
  - `PersonaInfluenceActionDefinition` (costs, cooldowns, required tiers)
  - `EconomicCenterConfig`, `TradeRouteConfig`
- Hot reload support through `ResourceWatcherService` integration; propagate reload events to Simulation Service
  - In-process work queue options (e.g., channel capacity, backpressure thresholds)

---

## 7. Workflows

### 7.1 Dominion Turn Lifecycle

1. WorldEngine enqueues `RunDominionTurnCommand`
2. Simulation Service persists `DominionTurnJob` (Queued) and emits `DominionTurnJobQueuedEvent`
3. Job processor claims job, sets status to Running, emits `DominionTurnJobStartedEvent`
4. Executes ordered scenarios (import balancing, taxation, civic stat updates, persona actions)
5. For each scenario, publishes resulting commands/events back to WorldEngine
6. On success, marks job Completed and emits `DominionTurnCompletedEvent`; on failure, marks Failed and emits `DominionTurnJobFailedEvent` with diagnostics

### 7.2 Settlement Civic Stat Aggregation

- Consume recent domain events, apply weighted aggregation rules
- Publish `SettlementCivicStatsUpdatedEvent` containing snapshot payload for WorldEngine to persist and project
- Cache computed snapshot locally for follow-up scenarios; dashboards update when WorldEngine processes the event

### 7.3 Persona Influence Processing

- `GrantInfluenceCommand` increments in-memory/session balance and emits `InfluenceGrantedEvent`; WorldEngine persists authoritative ledger entry
- `SpendInfluenceCommand` validates balance (using cached ledger + WorldEngine projection); on success emits `InfluenceSpentEvent` and queues persona action
- Resolve persona actions for simulation (intrigue, diplomacy, etc.), returning outcomes in `PersonaActionResolvedEvent` for WorldEngine to apply side effects

### 7.4 Market Pricing Adjustments

- Receive `RepriceMarketInventoryCommand` with demand signals and ItemSnapshot base price
- Calculate multiplier, include advisory payload in `MarketPricesAdjustedEvent`; WorldEngine records history and applies adjustments

---

## 8. Non-Functional Requirements

- **Reliability**: Exactly-once processing per dominion job; retries with exponential backoff and DLQ for poisoned messages.
- **Resilience**: Circuit breakers should suspend new work intake and surface alerts when WorldEngine or the event bus is unavailable, preventing runaway job accumulation.
- **Performance**: Jobs must complete within configured SLA (default 5 minutes) to avoid backlog; monitor channel depth and job latency.
- **Scalability**: Target one container per environment (pre-prod, prod). Support manual horizontal scale-out if needed, using distributed locks to prevent duplicate job execution, but no automated endless scaling is required.
- **Observability**: Structured logs, metrics (job duration, failures, channel occupancy), distributed tracing across portal/event bus.
- **Security**: Use service account credentials with least privilege DB access; secure event bus connections (TLS, auth tokens).

---

## 9. Testing Strategy

- Unit tests for aggregation rules, influence ledger operations, scenario execution
- Integration tests using in-memory event bus and PostgreSQL test container
- Contract tests validating command/event schemas against WorldEngine shared library
- BDD specs (code-first) under `AmiaReforged.Simulation.Tests` mirroring Economy design:
  - `DominionTurnSpecs`
  - `SettlementCivicStatSpecs`
  - `InfluenceActionSpecs`
  - `MarketPricingSpecs`
- Load tests simulating concurrent dominion jobs and persona actions

---

## 10. Deployment & Operations

- Docker image built from solution pipeline; versioned alongside WorldEngine
- Kubernetes/Compose manifest defines:
  - Simulation Service container
  - Shared secret mounts (DB credentials, message broker)
  - Config volume pointing to definition files
- Health endpoints:
  - `/health/startup` (ensures dependencies reachable)
  - `/health/live` (confirm worker loop running)
  - `/health/ready` (portal/event bus subscriptions active)
- Graceful shutdown: drain in-progress jobs, checkpoint state, requeue unfinished work
  - Graceful shutdown: drain in-process channel queues, checkpoint state, requeue unfinished work

---

## 11. Decisions & Remaining Questions

**Decisions**
- No automated scale-out is required; each environment (pre-prod, prod) runs its own Simulation Service instance, and they operate on fully separate data sets.
- Circuit breakers are mandatory to pause job intake and surface alerts when WorldEngine or the portal is unavailable.
- Simulation validates the inputs of commands it receives (e.g., influence spend) but relies on WorldEngine to enforce broader domain invariants before issuing those commands.
- Simulator and WorldEngine share the `PwEngine` PostgreSQL database for read access; WorldEngine remains the authoritative writer for domain state, while Simulation persists only its own orchestration metadata.

**Remaining Questions**
- None at this time.

---

## 12. Next Steps

1. Scaffold Simulation Service project with shared domain references and portal integration
2. Define EF Core models/migrations for simulation-owned tables (dominion jobs, work items, optional inbox/outbox)
3. Implement event bus consumer/producer infrastructure with resiliency policies
4. Build initial dominion turn scenario pipeline and civic stat aggregation
5. Integrate automated tests, in-process channel workers, and container build pipeline
6. Pilot deployment in staging alongside WorldEngine to validate end-to-end workflows
