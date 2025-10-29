# Economy Game Design

**Authors**: Amia Staff

---

## 1. Purpose & Scope

This document captures a cohesive plan for extending the WorldEngine economy to cover housing, rentals, civic census flow, banking, and marketplace operations. It complements `ARCHITECTURE.md` and focuses on domain design, CQRS orchestration, persistence strategy, and BDD-style validation. The scope includes:

- Census houses and settlement population onboarding
- Property ownership and rental lifecycles
- CoinHouse-powered financial services and repossession flows
- Market stall operations, delinquency handling, and NPC liquidation processes
- Settlement-level demand, supply, and trade routing (import/export)
- Reuse of existing WorldEngine abstractions wherever possible
- Off-process Simulation Service that executes dominion turns and heavy analytics without impacting the NWN server

---

## 2. Guiding Principles

- **Reuse First**: Extend `Economy`, `Regions`, `Organizations`, and `SharedKernel` value objects instead of creating parallel models. Prefer new aggregates only when invariants require them.
- **Persona-Centric**: Every actor (players, guilds, governments, NPC caretakers) must be addressable through existing `PersonaId` abstractions. Rental agreements, ownership, and stall operations should remain persona-agnostic.
- **Policy-Driven**: Conditions such as citizenship, criminal status, or guild reputation are configurable policies resolved at runtime using existing repositories (`IReputationRepository`, `IOrganizationRepository`, `IPersonaRepository`).
- **Event-Sourced Integration**: Use domain events to broadcast state changes (e.g., `PropertyForeclosedEvent`, `MarketStallDelinquentEvent`) so dependent subsystems (notifications, NWScript glue) remain loosely coupled.
- **Deterministic Simulation**: Economic simulations (demand, supply, stall turnover) should run on scheduler-friendly services with persisted checkpoints to avoid NPC attrition or double processing.
- **Testable by Design**: Capture behavior with code-first BDD specs under `AmiaReforged.PwEngine/Tests`, drive implementation through failing specs, and back solutions with unit tests for value objects.
- **Process Isolation**: Offload heavy analytics and dominion turns to the Simulation Service so the NWN server remains responsive while still sharing domain contracts.

---

## 3. Bounded Context Overview

| Context | Purpose | Primary Aggregates | Reuse Targets |
| --- | --- | --- | --- |
| **Census & Citizenship** | Register personas with settlements; manage civic status | `SettlementCensus`, `CitizenshipRecord` | `Regions`, `Organizations`, `PersonaId`, `CharacterStatRepository` |
| **Property Lifecycle** | Own, rent, foreclose houses/buildings | `Property`, `PropertyAgreement` | `PersistentCharacterRepository`, `CoinHouse` entities, `StoredItemRepository` |
| **Banking & CoinHouses** | Custody funds/items, enforce eligibility policies | `CoinHouseAccount`, `EscrowLot`, `RepossessionTicket` | `Economy` commands, `CoinHouse` DB tables, `Quantity` value object |
| **Marketplaces** | Operate stalls, collect fees, manage delinquency | `MarketStall`, `StallContract`, `StallInventory` | `Economy.Transactions`, `Organizations`, `ItemDefinition` loader |
| **Settlement Economy** | Aggregate demand/supply, import/export routing | `SettlementEconomyState`, `TradeRoute` | `Regions`, `ResourceNodes`, `Industries`, `Codex` event hooks |
| **Simulation Service** | Execute dominion turns and heavy analytics off-process | `DominionTurnJob`, `SimulationScenario` | Event bus contracts, shared EF Core models, projection snapshots |

Each context remains within WorldEngine but slices handlers and repositories under clear namespaces (`Features/WorldEngine/Economy/...`).

---

## 4. Domain Model Sketches

### 4.1 Shared Value Objects

- `PropertyId`, `StallId`, `SettlementId` (reuse of existing typed IDs where available)
- `AgreementStatus` enum: Pending, Active, Suspended, Terminated, Foreclosed
- `CitizenshipTier` value object: Visitor, Resident, Citizen, Nobility (maps to government ranks)
- `GovernmentRankId` and `GovernmentTitle` for elected/appointed positions backed by organizations
- `EligibilityPolicyId` linking to configuration JSON entries that declare rules
- `MaintenanceFee` record struct wrapping `Quantity` and cadence (`Duration`)
- `EscrowRetentionPeriod` record struct encapsulating real-world duration defaults per settlement
- `DemandScore`, `PopulationMetric` to quantify settlement simulation results
- `LoyaltyScore`, `SecurityScore`, `ManpowerLevel`, `ProsperityScore`, `HappinessIndex`
- `MilitaryMight`, `ArcanePower`, `DefenseRating` for strategic readiness tracking
- `InfluencePoints` value object representing persona action currency

### 4.2 Aggregates & Entities

```text
Property (Aggregate)
  - PropertyId
  - LocationTag (reuse Region/Area tagging)
  - PropertyType (Residence, MarketHall, Warehouse)
  - Agreements (Ownership, Rental) -> Value object with PersonaId, status
  - InventoryHoldId (CoinHouse escrow reference)
  - RetentionPolicy (EscrowRetentionPeriod aligned to settlement defaults)
  - Events: PropertyListed, AgreementActivated, AgreementDefaulted, PropertyForeclosed

MarketStall (Aggregate)
  - StallId, LinkedMarketId, OwningOrganizationId
  - FeeSchedule (MaintenanceFee)
  - RevenueAccountId (CoinHouse ledger)
  - Inventory (List of StoredItemId)
  - PricingModel (base ItemSnapshot price + demand multiplier accumulator)
  - Events: StallRegistered, StallPaymentProcessed, StallDelinquent, StallLiquidated

SettlementEconomyState (Aggregate)
  - SettlementId, LinkedRegionIds, PopulationMetrics
  - DemandVectors (per ItemCategory)
  - SupplyVectors (harvest output, industry production)
  - TradeRoutes (imports/exports summary)
  - DominionTurnState (queue of pending sub-entity turns per government)
  - CivicStats (Loyalty, Security, Manpower, Prosperity, Happiness, Military Might, Arcane Power, Defense Rating)
  - Events: SettlementDemandUpdated, TradeRouteEstablished, PopulationAdjusted

PersonaInfluenceLedger (Entity/Owned Type)
  - PersonaId
  - InfluenceBalance (InfluencePoints)
  - RecentTransactions (history of earned/spent actions)
  - Methods: GrantInfluence, SpendInfluence, CanAfford

CensusHouse (Entity within Settlement)
  - SettlementId, IntakePolicies, RegistrarPersonaId
  - Methods: RegisterCitizen, RevokeCitizenship
```

Wherever possible, embed existing value objects (`Quantity`, `GoldAccount`, `RegionTag`) to enforce consistency.

### 4.3 Economic Center Hierarchy

- **Area**: Smallest unit (e.g., individual NWN area). Areas declare a `SettlementId` linkage in JSON and inherit its policies automatically.
- **Settlement**: Operates census houses, properties, and markets. Each settlement references the `SettlementEconomyState` aggregate and declares an `economicCenterWeight` used when resolving regional primacy.
- **Region**: Groups settlements. The settlement with the highest economic weight becomes the **regional economic center** and drives import/export routing for the region. A change in weights raises a `RegionalEconomicCenterChangedEvent` to rebalance trade routes.
- **Territory/Government Capital**: Territories aggregate one or more regions owned by a government persona. Capitals are explicitly declared in JSON and act as override centers when government policy demands centralized control (e.g., taxes, embargoes).
- **Fallback Rules**: If a settlement becomes inactive (no population, no market activity), control cascades upward, allowing the region or territory capital to assume trade obligations until a new settlement qualifies.
- **Dominion Turns**: Governments execute scheduled dominion turns that iterate through every subordinate entity (regions, settlements, guild-managed markets) in background jobs, ensuring high-level policy changes propagate without blocking gameplay.
- **Telemetry Dashboards**: DM Tool NUI and admin-only windows consume projection data in-process to provide live control panels without external telemetry infrastructure.

This "nesting doll" structure keeps computations localized (area → settlement) while providing deterministic roll-up behavior (settlement → region → territory) without duplicating aggregates.

### 4.4 Simulation Service Domain

```text
DominionTurnJob (Aggregate)
  - DominionTurnId
  - GovernmentId
  - ScheduledFor (DateTimeOffset)
  - Status (Pending, Running, Completed, Failed)
  - WorkItems (queue of SimulationScenario references)
  - Events: DominionTurnJobQueued, DominionTurnJobStarted, DominionTurnJobCompleted, DominionTurnJobFailed

SimulationScenario (Entity)
  - ScenarioId
  - TargetAggregateId (Settlement, Region, Organization)
  - ScenarioType (ImportDemandBalancing, TaxationReview, MilitaryUpkeep, Immigration, CivicStatAggregation, PersonaInfluence)
  - Inputs (projection snapshots, policy configs, ItemSnapshots, actor happiness metrics)
  - Outputs (commands/events emitted back to WorldEngine via portal)
```

The Simulation Service reuses shared value objects but encapsulates job orchestration so the NWN process only receives finalized events and projections.

---

## 5. CQRS Design

### 5.1 Commands

- `RegisterCitizenCommand(SettlementId, PersonaId, EligibilityPolicyId)`
- `StartPropertyAgreementCommand(PropertyId, PersonaId, AgreementType, EligibilityPolicyId)`
- `ProcessPropertyPaymentCommand(AgreementId, Quantity)`
- `FlagAgreementDelinquencyCommand(AgreementId, Reason)`
- `EscrowPropertyContentsCommand(PropertyId)`
- `RegisterMarketStallCommand(StallId, OrganizationId, FeeSchedule)`
- `ProcessDailyStallFeesCommand(SettlementId)` (scheduled)
- `SettleMarketPurchaseCommand(StallId, PersonaId, ItemId, Quantity)`
- `UpdateSettlementDemandCommand(SettlementId, DemandSnapshot)`
- `ScheduleTradeRouteCommand(SettlementId, TargetMarketId, Manifest)`
- `RepriceMarketInventoryCommand(SettlementId, ItemSnapshotId, DemandSignal)`
- `RunDominionTurnCommand(GovernmentId)` (background scheduler entry point)
- `UpdateSettlementCivicStatsCommand(SettlementId, CivicStatSnapshot)`
- `GrantInfluenceCommand(PersonaId, InfluencePoints, Reason)`
- `SpendInfluenceCommand(PersonaId, InfluencePoints, PersonaActionId)`
- `QueuePersonaActionCommand(PersonaId, PersonaActionRequest)`

Each command handler should live under `WorldEngine.Application.*` and reuse existing repositories via interfaces. Eligibility enforcement belongs in domain services invoked by handlers.

### 5.2 Queries

- `GetCitizenStatusQuery(PersonaId)`
- `GetAvailablePropertiesQuery(SettlementId, AgreementType)`
- `GetPropertyAgreementDetailQuery(AgreementId)`
- `GetStallDashboardQuery(StallId)`
- `GetSettlementEconomyStateQuery(SettlementId)`
- `ListTradeRoutesQuery(SettlementId)`
- `GetSettlementCivicStatsQuery(SettlementId)`
- `GetPersonaInfluenceQuery(PersonaId)`
- `ListPersonaActionsQuery(PersonaId)`

Queries rely on read models shaped for UI/NWN scripts. Prefer projection via EF Core or in-memory caches.

### 5.3 Events

- `CitizenRegisteredEvent`, `CitizenshipRevokedEvent`
- `PropertyAgreementActivatedEvent`, `PropertyAgreementDefaultedEvent`, `PropertyForeclosedEvent`
- `PropertyContentsEscrowedEvent`, `EscrowExpiredEvent`
- `MarketStallPaymentProcessedEvent`, `MarketStallDelinquentEvent`, `MarketStallLiquidatedEvent`
- `MarketPurchaseCompletedEvent`
- `SettlementDemandUpdatedEvent`, `TradeRouteScheduledEvent`
- `MarketPricesAdjustedEvent`
- `DominionTurnJobQueuedEvent`, `DominionTurnCompletedEvent`, `DominionTurnJobFailedEvent`
- `SettlementCivicStatsUpdatedEvent`
- `InfluenceGrantedEvent`, `InfluenceSpentEvent`, `PersonaActionQueuedEvent`, `PersonaActionResolvedEvent`

Existing event bus infrastructure (`IEventBus`, `[ServiceBinding]`) handles publication and discovery.

---

## 6. Persistence Strategy

### 6.1 Database

- Extend `PwEngineContext` with new tables: `PersistentProperty`, `PersistentPropertyAgreement`, `PersistentStall`, `PersistentTradeRoute`, `PersistentSettlementEconomy`, `PersistentDominionTurn`.
- Reuse `PersistentCharacterTrait`, `Organization`, and `CoinHouse` tables for joins. For escrow items, reuse `StoredItem` with additional `EscrowTicketId` column.
- `EscrowTicket` table to track custody start date, expiry, and policy actions.
- `MarketPriceHistory` table capturing ItemSnapshot base price, demand multiplier, and timestamps to support analytics and rollback.
- `SettlementCivicStats` table storing latest aggregate values plus historical trail for audit.
- `PersonaInfluenceLedger` table capturing balances and transactions (`InfluenceLedgerEntry`).
- Simulation Service consumes the same PostgreSQL schema/context via its own DbContext, using optimistic concurrency tokens on shared aggregates to avoid write contention.

### 6.2 JSON Configuration

- Extend `WorldEngineConfig` to include `EligibilityPolicies`, `SettlementEconomyConfig`, and `PropertyTemplates`.
- Configuration should declare policy composition (e.g., `requiresCitizenship`, `maxCriminalSeverity`, `approvedGuildTags`).
- Settlements declare `economicCenterWeight`, `importPreferences`, `exportPreferences` for supply/demand simulation, plus `defaultEscrowRetention` (default 60 real-world days) and `citizenshipTierPolicies`.
- Regions specify optional `hierarchyOverrides` and tie-breaker strategies (e.g., population-first, revenue-first) for selecting economic centers. Territories define `capitalSettlementId`, default embargo rules, and `dominionTurnCadence`.
- Define `SettlementCivicStatWeights` and thresholds (e.g., loyalty decay rates, unrest triggers) alongside persona influence action costs and cooldowns.

### 6.3 File Loading

Leverage existing `EconomyLoaderService` pattern:

1. Add `PropertyDefinitionLoadingService` using `FileSystemConfig` paths.
2. Register new definitions via `IDefinitionLoader` for census policies, market fee schedules, and dominion turn cadence profiles.
3. Load `SettlementCivicStatDefinition` files (stat weights, decay curves, thresholds) and `PersonaInfluenceActionDefinition` catalogs.
4. Surface reusable `ItemSnapshot` data (base item price, stack size, tags) through a lightweight facade so that pricing logic remains domain-pure.

### 6.4 Projections

- Create projection tables or caches for UI dashboards (e.g., `SettlementEconomyView`, `PropertyListingView`).
- Use `ResourceWatcherService` to reload config when definitions change.
- Maintain a lightweight `EconomicCenterView` projection to quickly identify the current center per region and territory when running scheduled jobs.
- Build DM Tool NUI and admin dashboard view models on top of these projections to surface health metrics, demand signals, and pricing adjustments without leaving the main process.
- Replicate projection refresh notifications to the Simulation Service via the simulation portal so off-process jobs always operate on current definitions.
- Introduce `SettlementCivicStatsView` and `InfluenceLedgerView` projections so dashboards and persona action planners can operate on current aggregate data.

---

## 7. Eligibility & Policy Engine

- Implement `IEconomicEligibilityService` in `WorldEngine.Services` that evaluates policies against persona data.
- Policies composed from reusable predicates:
  - `HasCitizenship(SettlementId)` via `SettlementCensus`
  - `MeetsCitizenshipTier(CitizenshipTier requiredTier)` with support for government ranks/nobility overrides
  - `HasCleanCriminalRecord()` via `ICharacterStatRepository`
  - `IsInGoodStandingWithOrganization(OrganizationId)` via `IReputationRepository`
  - `HasSufficientFunds(Quantity)` via `CoinHouseAccount`
- Tiered citizenship enables special-case policies (e.g., nobles bypass certain escrow requirements, elected officials unlock government housing pools).
- Configuration declares predicate identifiers; service resolves them at runtime.

---

## 8. Workflows & Event Flows

### 8.1 Property Rental Lifecycle

1. `StartPropertyAgreementCommand` validates eligibility.
2. Agreement activates; publishes `PropertyAgreementActivatedEvent`.
3. Scheduled job triggers `ProcessPropertyPaymentCommand` daily/weekly.
4. On failed payment, `FlagAgreementDelinquencyCommand` issued.
5. After grace period, `EscrowPropertyContentsCommand` moves items to CoinHouse escrow.
6. Escrow expiration triggers liquidation (`EscrowExpiredEvent`).

### 8.2 Market Stall Operations

- `RegisterMarketStallCommand` binds stall to organization and fee schedule.
- `ProcessDailyStallFeesCommand` aggregates profits held in CoinHouse accounts.
- If insufficient funds, raise `MarketStallDelinquentEvent` and move inventory to escrow.
- Purchase flow uses `SettleMarketPurchaseCommand`, which debits buyer and credits stall ledger.

### 8.3 Settlement Demand Simulation

- `UpdateSettlementDemandCommand` executed by scheduler using data from harvesting, industries, and player activity logs.
- Event triggers recalculation of `TradeRoute`s; commands schedule shipments to other settlements or off-shard markets.
- NPC population adjustments respond to demand thresholds while respecting attrition guardrails (soft caps, immigration backfill).
- When regional centers change, raise `EconomicCenterRebalancedEvent` and invalidate cached trade manifests so that downstream handlers recompute import/export obligations.

### 8.4 Dominion Turn Processing

- WorldEngine schedules `RunDominionTurnCommand` messages that are forwarded through the simulation portal to the dedicated Simulation Service (separate project/container).
- The Simulation Service orchestrates the hierarchy (territory → region → settlement → subordinate organizations) off-process, executing policy-specific actions without impacting the NWN server thread pool.
- Simulation results publish events (e.g., levy taxes, adjust garrison budgets, reprioritize imports) back onto the event bus, where existing command handlers persist changes.
- Completion emits `DominionTurnCompletedEvent` summarizing outcomes for downstream integrations (webhooks, NWScript notifications).
- Economic center rebalancing happens in bulk at the conclusion of dominion turns, allowing trade manifests to settle before the next turn cycle.
- This topology keeps heavy data crunching in a dockerized Simulation Service today while preserving the same contracts should we scale to additional instances later.

### 8.5 Settlement Stats & Persona Influence

- Simulation Service aggregates recent events (economy, crime, player actions) into `SettlementCivicStats` snapshots and publishes `SettlementCivicStatsUpdatedEvent`.
- WorldEngine persists the snapshot, updates projections, and notifies DM/Admin dashboards.
- Personas earn `InfluencePoints` via event handlers (e.g., completing civic quests, fulfilling trade quotas) that emit `InfluenceGrantedEvent`.
- Spending influence issues `SpendInfluenceCommand`, validates balance, and dispatches persona action requests (`PersonaActionQueuedEvent`) for the Simulation Service to resolve (intrigue, diplomacy, etc.).
- Resolved persona actions publish `PersonaActionResolvedEvent`, which may modify settlement stats, trigger organization state changes, or adjust character reputations.

---

## 9. BDD Test Strategy

Add code-first BDD specs under `AmiaReforged.PwEngine/Tests/Economy` (using the existing test harness patterns):

- `PropertyRentalSpecs`
  - scenario: successful rental onboarding validates eligibility and activates agreement
  - scenario: missed payments transition agreement to foreclosure and escrow contents
- `CitizenRegistrationSpecs`
  - scenario: player rejected due to criminal status flag
  - scenario: NPC granted citizenship unlocks property eligibility predicates
- `CitizenshipTierSpecs`
  - scenario: resident tier blocks government housing until citizenship granted
  - scenario: nobility tier bypasses standard escrow collateral requirements
- `MarketStallManagementSpecs`
  - scenario: stall fees auto-deduct from uncollected profits in CoinHouse
  - scenario: delinquent stall inventory moves to escrow with liquidation clock
- `MarketPricingSpecs`
  - scenario: demand surge applies multiplier over ItemSnapshot base price
  - scenario: demand drop floors price at configurable minimum margin
- `SettlementDemandSpecs`
  - scenario: demand spike triggers import trade route scheduling
  - scenario: population adjustments respect attrition guardrails and immigration backfill
- `SettlementCivicStatSpecs`
  - scenario: aggregated events adjust loyalty and security according to configured weights
  - scenario: unrest spike triggers downstream security warnings and DM dashboard alerts
- `DominionTurnSpecs`
  - scenario: dominion turn processes region and settlement actions asynchronously
  - scenario: dominion turn summary event publishes aggregated results to event bus portal
- `EconomyDashboardSpecs`
  - scenario: DM Tool NUI dashboard aggregates projections for settlements and stalls
  - scenario: admin control panel surfaces dominion turn summaries and price deltas
- `InfluenceActionSpecs`
  - scenario: persona earns influence from successful diplomacy and can spend it on intrigue
  - scenario: insufficient influence balance blocks high-cost action and emits denial event

Specs execute directly against command handlers and repositories; prefer fluent assertion helpers already present in the PwEngine test harness to keep scenarios readable.

---

## 10. Implementation Roadmap

1. **Foundation (Sprint 1)**
   - Define new value objects and aggregates in `WorldEngine.SharedKernel` and `WorldEngine.Economy` namespaces.
   - Update EF Core migrations: tables for properties, agreements, stalls, settlements.
   - Implement eligibility service with mock predicates.
  - Scaffold the Simulation Service project (separate process) with shared library references and event bus portal contracts.

2. **Property & Census (Sprint 2)**
   - Implement census registration commands and queries.
   - Build property agreement command handlers and events.
   - Create BDD specs for registration and rental flows.

3. **Marketplaces (Sprint 3)**
   - Implement stall registration, fee processing, and purchase settlement.
   - Integrate with CoinHouse accounts for profits and delinquency.
   - Add BDD specs for stall fees and liquidation.

4. **Settlement Simulation (Sprint 4)**
   - Implement demand/supply state, trade route scheduling, and import/export logic.
   - Introduce scheduler services with persisted checkpoints.
   - Add code-first BDD specs for demand-driven imports and economic center rebalancing.
  - Implement Simulation Service dominion turn processor and ensure event bus round-trip works across docker containers.

5. **Polish & Integration (Sprint 5)**
   - Hook events into NWScript bridge.
   - Add UI/Player tool enhancements for dashboards.
   - Run end-to-end PwEngine test suites; stabilize migration scripts.
  - Load test Simulation Service scaling and validate failover with queued dominion jobs.

---

## 11. Integration Points & Reuse Summary

- **CoinHouse**: Use existing transaction and account entities for property payments, stall profits, and escrow holdings.
- **StoredItemRepository**: Reuse for escrow inventories; add labels for escrow ticket tracking.
- **Regions & Settlements**: Extend JSON definitions to tag census houses and economic centers; reuse `RegionTag` for area-linked properties.
- **Organizations**: Market stalls and property management leverage existing membership and reputation models.
- **Harvesting & Industries**: Feed settlement demand and supply metrics using existing event streams (`ResourceHarvestedEvent`, `ProductionRecordedEvent`).
- **Player Tools**: Extend UI presenters to display property agreements and stall dashboards without duplicating NWScript integration logic.
- **Event Bus Portal**: Maintain a single integration seam so potential external simulators/microservices can publish and subscribe via a mediator service without bypassing WorldEngine invariants.
- **Simulation Portal**: Route dominion turns and other heavy simulations to the dedicated Simulation Service project (dockerized) while mirroring event bus contracts so workloads stay decoupled from the NWN process.
- **Economy Dashboards**: Surface telemetry through in-process control panels exposed via new DM Tool NUI screens and admin-only (Zoltan/Jes) dashboards so live tuning stays integrated with gameplay.
- **Persona Systems**: Integrate influence earning/spending with existing reputation and organization membership services to avoid duplicating persona context.

---

## 12. Decisions & Open Questions

**Decisions**
- Tiered citizenship is official: tiers include Visitor, Resident, Citizen, and Nobility/Government Rank; government titles map to special-purpose policies and housing queues.
- Escrow retention is settlement dependent with a default of 60 real-world days unless overridden.
- Market pricing is demand driven, anchored by ItemSnapshot base prices exposed through the domain facade.
- Dominion turn and simulation workloads run in a dedicated Simulation Service project (dockerized) reachable via the simulation portal, keeping the NWN server free from heavy background processing.
- Territory-level dominion turns run asynchronously in the Simulation Service, cascading through subordinate entities and performing economic center rebalancing in bulk via the event bus.

**Open Questions**
- What operational metrics should trigger scaling the Simulation Service horizontally (e.g., queue depth, job latency)?

---

## 13. Next Steps

- Review this design with economy stakeholders.
- Prioritize BDD spec classes under `AmiaReforged.PwEngine/Tests/Economy` and scaffold any missing harness utilities.
- Align NWScript endpoints with planned commands/queries to avoid API mismatches.
- Start iterating on Sprint 1 tasks, keeping event contracts versioned and backward compatible.
