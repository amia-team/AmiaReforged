# WorldSimulator BDD Testing Path

**Document Owners**: WorldSimulator Team
**Last Updated**: October 29, 2025
**Status**: Requirements Definition

---

## Purpose

This document outlines the complete BDD testing strategy for the WorldSimulator service, with emphasis on **Persona Influence & Action System**. These scenarios cement requirements in executable specifications that drive implementation.

**Key Principle**: WorldSimulator **processes** data locally, **requests** data from WorldEngine when it's the authoritative source.

---

## 1. Persona Influence Ledger Scenarios

### 1.1 Earning Influence

```gherkin
Feature: Persona Influence Earning
    As a persona in the game world
    I want to earn influence through actions
    So that I can spend it on intrigue, diplomacy, and other actions

Scenario: Earn influence from successful civic quest completion
    Given a persona "Lord Blackwood" with PersonaId "aaaa-bbbb-cccc-dddd"
    And the persona has 100 influence points
    When WorldEngine publishes "CivicQuestCompletedEvent" granting 50 influence
    Then the persona influence balance should be 150
    And an "InfluenceGrantedEvent" should be published
    And the transaction history should record "Civic Quest: Secure Trade Route"

Scenario: Earn influence from settlement loyalty milestone
    Given a settlement "Cordor" with SettlementId "1111-2222-3333-4444"
    And a persona "Mayor Valencia" is the elected leader
    And Cordor's loyalty score increases from 60 to 80
    When the civic stats are aggregated
    Then Mayor Valencia should earn 25 influence
    And the reason should be "Settlement Loyalty Milestone: Good (80)"

Scenario: Influence grant is idempotent
    Given a persona has received influence from event "quest-reward-123"
    When the same event is replayed due to message retry
    Then the influence should not be granted again
    And the duplicate should be logged but ignored
```

### 1.2 Spending Influence

```gherkin
Scenario: Spend influence on successful action
    Given a persona "Duchess Ravencroft" with 500 influence points
    And the persona queues an "Intrigue" action costing 100 influence
    When the action is validated
    Then the influence balance should be reduced to 400
    And an "InfluenceSpentEvent" should be published
    And the action should be queued for processing

Scenario: Reject action with insufficient influence
    Given a persona "Baron Thorne" with 30 influence points
    And the persona attempts to queue a "Diplomacy" action costing 50 influence
    When the action is validated
    Then the action should be rejected
    And the rejection reason should be "Insufficient Influence: Required 50, Available 30"
    And no "InfluenceSpentEvent" should be published
    And the influence balance should remain 30

Scenario: Partial refund on failed action
    Given a persona spends 100 influence on an intrigue action
    And the action fails during resolution
    And the action definition specifies "50% refund on failure"
    When the action failure is processed
    Then 50 influence should be refunded to the persona
    And an "InfluenceRefundedEvent" should be published
```

---

## 2. Persona Action Queue Scenarios

### 2.1 Queue Management

```gherkin
Feature: Persona Action Queue Management
    As a persona
    I want to queue multiple actions
    But be limited to prevent spam and maintain balance

Scenario: Queue action within limit
    Given a persona "Spymaster Vex" with an empty action queue
    And the queue limit is 5 actions
    When the persona queues 3 intrigue actions
    Then all 3 actions should be queued successfully
    And the queue count should be 3

Scenario: Reject action when queue is full
    Given a persona has 5 actions already queued (at limit)
    When the persona attempts to queue another action
    Then the action should be rejected
    And the rejection reason should be "Action queue full: 5/5"
    And no influence should be spent

Scenario: Queue slot freed after action processes
    Given a persona has 5 actions queued (at limit)
    When one action completes processing
    Then the queue count should be 4
    And the persona can queue a new action

Scenario: Actions process in FIFO order
    Given a persona queues actions in order: A, B, C
    When the simulation worker processes the queue
    Then actions should resolve in order: A, B, C
```

### 2.2 Action Cooldowns

```gherkin
Scenario: Action cooldown prevents spam
    Given a persona successfully completes "Spy on Merchant Guild" action
    And the action definition has a 24-hour cooldown
    When the persona attempts the same action 12 hours later
    Then the action should be rejected
    And the rejection reason should be "Action on cooldown: 12h remaining"

Scenario: Different actions don't share cooldowns
    Given a persona completes "Spy on Merchant Guild"
    And "Spy on Merchant Guild" has a 24-hour cooldown
    When the persona queues "Sabotage Trade Route"
    Then the action should be queued successfully
    Because cooldowns are per-action-type, not global
```

---

## 3. Action Definition & Configuration

### 3.1 JSON-Configured Actions

```gherkin
Feature: Action Definition Loading
    As a game designer
    I want to define persona actions in JSON
    So that I can tune costs, success rates, and effects without code changes

Scenario: Load intrigue action definition from JSON
    Given a JSON file "PersonaActions/Intrigue/SpyOnTarget.json":
        """
        {
          "actionId": "spy-on-target",
          "actionType": "Intrigue",
          "displayName": "Spy on Target",
          "influenceCost": 100,
          "cooldownHours": 24,
          "successFactors": {
            "intrigueSkill": { "weight": 0.4, "min": 0, "max": 100 },
            "targetReputation": { "weight": 0.3, "inverseScale": true },
            "settlementSecurity": { "weight": 0.2, "inverseScale": true },
            "renownDifference": { "weight": 0.1 }
          },
          "effects": {
            "onSuccess": [
              { "type": "GrantInformation", "payload": "target-secret-data" },
              { "type": "ReduceTargetReputation", "amount": 5 }
            ],
            "onFailure": [
              { "type": "ReduceActorReputation", "amount": 10 },
              { "type": "AlertTarget", "chance": 0.75 }
            ]
          }
        }
        """
    When the action definitions are loaded
    Then the "spy-on-target" action should be available
    And the influence cost should be 100
    And the cooldown should be 24 hours

Scenario: Validate action definition on startup
    Given an invalid action definition with negative influence cost
    When the simulator service starts
    Then the service should fail to start
    And the error should indicate "Invalid action definition: Influence cost cannot be negative"
```

### 3.2 Success Calculation

```gherkin
Feature: Action Success Calculation
    As the simulation service
    I want to calculate action success based on configured factors
    So that outcomes are deterministic and tunable

Scenario: Calculate success chance for intrigue action
    Given an intrigue action "Spy on Reginald" with success factors:
        | Factor              | Weight | ActorValue | TargetValue |
        | IntrigueSkill       | 0.4    | 75         | -           |
        | TargetReputation    | 0.3    | -          | 80 (inverse)|
        | SettlementSecurity  | 0.2    | -          | 60 (inverse)|
        | RenownDifference    | 0.1    | 50         | 40          |
    When the success chance is calculated
    Then the base success chance should be approximately 65%
    And the calculation breakdown should be logged for debugging

Scenario: Request actor intrigue skill from WorldEngine
    Given a persona "Shadowblade" queues an intrigue action
    When the simulation processes the action
    Then the simulator should request "GET /api/simulation/persona/{id}/stats"
    And WorldEngine should respond with:
        """
        {
          "personaId": "...",
          "intrigueSkill": 85,
          "diplomacySkill": 45,
          "renown": 120,
          "reputation": { "Cordor": 75, "MerchantGuild": -20 }
        }
        """
    And the simulator should use intrigueSkill=85 in calculations

Scenario: Request target reputation from WorldEngine
    Given an action targets another persona "Reginald"
    When calculating success factors
    Then the simulator should request "GET /api/simulation/persona/{targetId}/reputation"
    And use the response in the "TargetReputation" factor (inverse scale)
```

---

## 4. Dominion Context & Location

### 4.1 Actions Occur in Settlement Context

```gherkin
Feature: Settlement-Contextual Actions
    As a simulation service
    I want actions to be processed in the context of their settlement
    So that local factors (security, loyalty, government) affect outcomes

Scenario: Intrigue action in high-security settlement
    Given a persona performs "Spy on Merchant" in settlement "Cordor"
    And Cordor has a security score of 90 (very high)
    When the action success is calculated
    Then the security score should reduce the success chance
    And the weight should be applied as configured (e.g., -0.2 weight)

Scenario: Request settlement security score from WorldEngine
    Given an action is queued in settlement "Cordor"
    When processing the action
    Then the simulator should request "GET /api/simulation/settlements/{id}/civic-stats"
    And WorldEngine should respond with:
        """
        {
          "settlementId": "1111-2222-3333-4444",
          "loyalty": 75,
          "security": 90,
          "prosperity": 80,
          "happiness": 70,
          "calculatedAt": "2025-10-29T14:30:00Z"
        }
        """
    And the simulator should use security=90 in success calculations

Scenario: Diplomacy action improves settlement loyalty
    Given a persona performs "Negotiate Trade Deal" in Cordor
    And the action succeeds
    When the action effects are applied
    Then the simulator should POST to "api/simulation/settlements/{id}/loyalty-adjustment"
    With payload: { "adjustment": +5, "reason": "Successful Trade Negotiation" }
    And WorldEngine should update Cordor's loyalty score
```

### 4.2 Government & Dominion Rules

```gherkin
Scenario: Action restricted by government policy
    Given settlement "Cordor" is ruled by "Kingdom of Amia"
    And the Kingdom has policy "embargo-intrigue-actions" active
    When a persona attempts an intrigue action in Cordor
    Then the action should be rejected
    And the rejection reason should be "Government Policy: Intrigue actions embargoed"

Scenario: Government leader has bonus to diplomacy in own territory
    Given persona "King Aldric" is the ruler of "Kingdom of Amia"
    And King Aldric performs a diplomacy action in his territory
    When calculating success chance
    Then King Aldric should receive a +15% bonus
    Because he has home-territory advantage
```

---

## 5. Action Resolution & Effects

### 5.1 Success & Failure Outcomes

```gherkin
Feature: Action Resolution
    As the simulation service
    I want to resolve actions and apply effects
    So that the game world reacts to persona activities

Scenario: Successful intrigue action reveals information
    Given persona "Agent Shadow" spies on "Merchant Lord Vex"
    And the success roll is 72 (above 65% threshold)
    When the action resolves
    Then the action should succeed
    And an "InformationRevealedEvent" should be published
    With payload containing Vex's secret trade routes
    And the simulator should POST to WorldEngine to store the information

Scenario: Failed intrigue action damages reputation
    Given persona "Clumsy Spy" attempts intrigue with 35% success chance
    And the success roll is 50 (below threshold)
    When the action resolves
    Then the action should fail
    And the actor's reputation in the settlement should decrease by 10
    And a 75% chance the target should be alerted
    And an "ActionFailedEvent" should be published

Scenario: Critical success grants bonus effects
    Given an action with 70% base success chance
    And the roll is 5 (critical success on ≤10)
    When the action resolves
    Then the action should critically succeed
    And bonus effects should be applied (e.g., double information, no detection risk)

Scenario: Critical failure has severe consequences
    Given an action with 40% base success chance
    And the roll is 98 (critical failure on ≥95)
    When the action resolves
    Then the action should critically fail
    And severe penalties should be applied (e.g., triple reputation loss, guaranteed detection)
```

### 5.2 Multi-Target Actions

```gherkin
Scenario: Diplomacy action affects multiple personas
    Given a diplomacy action "Broker Alliance" targeting 3 personas
    And each target has different reputation and willingness scores
    When the action resolves
    Then success should be calculated independently for each target
    And effects should be applied per-target based on individual success
    And the overall outcome should be "Partial Success" if 2/3 succeed

Scenario: Area-of-effect action in settlement
    Given an action "Incite Unrest" targeting settlement "Cordor"
    When the action succeeds
    Then the simulator should POST civic stat adjustments:
        | Stat       | Adjustment |
        | Loyalty    | -15        |
        | Happiness  | -10        |
        | Security   | -5         |
    And WorldEngine should update Cordor's civic stats
    And affected personas in Cordor should receive notifications
```

---

## 6. Integration with WorldEngine

### 6.1 Data Requests

```gherkin
Feature: WorldEngine Data Requests
    As the simulator
    I want to request data from WorldEngine via HTTP
    So that I use authoritative game state in calculations

Scenario: Request persona stats for action validation
    Given a persona queues an action
    When the simulator validates the action
    Then GET /api/simulation/persona/{id}/stats should be called
    And the response should be cached for 5 minutes
    And subsequent requests within 5 minutes should use the cache

Scenario: Request settlement data for context
    Given an action occurs in settlement "Cordor"
    When processing the action
    Then GET /api/simulation/settlements/{id}/civic-stats should be called
    And the response should include: loyalty, security, prosperity, happiness

Scenario: Request target persona data for opposed checks
    Given an action targets another persona
    When calculating success
    Then GET /api/simulation/persona/{targetId}/stats should be called
    And GET /api/simulation/persona/{targetId}/reputation should be called

Scenario: Handle WorldEngine unavailable gracefully
    Given the circuit breaker detects WorldEngine is down
    When an action requires WorldEngine data
    Then the action should be deferred (not failed)
    And the work item should remain in "Pending" status
    And retry should occur when circuit closes
```

### 6.2 Result Submission

```gherkin
Scenario: Submit action results to WorldEngine
    Given an action completes successfully
    When submitting results
    Then POST /api/simulation/persona-action-results should be called
    With payload:
        """
        {
          "actionId": "...",
          "personaId": "...",
          "targetId": "...",
          "settlementId": "...",
          "success": true,
          "effects": [
            { "type": "ReputationChange", "targetId": "...", "amount": -5 },
            { "type": "InformationGranted", "data": "..." }
          ],
          "timestamp": "2025-10-29T15:00:00Z"
        }
        """
    And WorldEngine should apply the effects to game state

Scenario: Retry failed result submission
    Given result submission fails with 500 error
    When the retry policy triggers
    Then the submission should be retried 3 times with exponential backoff
    And if all retries fail, the work item should be marked "Failed"
    And an alert should be sent to Discord webhook
```

---

## 7. Reputation & Renown

### 7.1 Reputation Checks

```gherkin
Feature: Reputation-Based Action Modifiers
    As the simulation service
    I want to use persona reputation in calculations
    So that well-known personas have advantages/disadvantages

Scenario: High reputation persona has diplomacy advantage
    Given persona "Beloved Mayor" has reputation 95 in "Cordor"
    And the persona performs a diplomacy action in Cordor
    When calculating success chance
    Then reputation should provide a +10% bonus
    Because the persona is trusted

Scenario: Low reputation persona suffers intrigue penalty
    Given persona "Known Criminal" has reputation 15 in "Cordor"
    And the persona performs intrigue in Cordor
    When calculating success chance
    Then reputation should impose a -15% penalty
    Because the persona is distrusted and watched

Scenario: Request reputation from WorldEngine
    Given an action requires reputation data
    When processing the action
    Then GET /api/simulation/persona/{id}/reputation?settlementId={sid} should be called
    And the response should be an integer 0-100
```

### 7.2 Renown Interactions

```gherkin
Scenario: Renown difference affects opposed actions
    Given actor "Famous Knight" has renown 150
    And target "Unknown Merchant" has renown 30
    And renown difference factor has weight 0.1
    When calculating success chance for an opposed action
    Then the renown advantage should add approximately +12% to success
    Because (150-30) * 0.1 = 12

Scenario: Equal renown provides no advantage
    Given actor and target both have renown 75
    When calculating success with renown factor
    Then the renown modifier should be 0%

Scenario: Request renown from WorldEngine
    Given an action requires renown comparison
    When processing the action
    Then GET /api/simulation/persona/{id}/renown should be called for both actor and target
```

---

## 8. Turn Processing Integration

### 8.1 Dominion Turn Action Resolution

```gherkin
Feature: Dominion Turn Action Processing
    As part of dominion turn execution
    I want queued persona actions to be resolved
    So that intrigue and diplomacy affect turn outcomes

Scenario: Process queued actions during dominion turn
    Given a government "Kingdom of Amia" has a dominion turn scheduled
    And 15 persona actions are queued in Kingdom territory
    When the dominion turn executes
    Then all queued actions should be processed in FIFO order
    And action results should be applied before settlement stat aggregation
    So that action effects influence civic stats

Scenario: Action cooldowns reset at turn boundary
    Given an action has a "per-turn" cooldown
    And the action was used in the previous turn
    When a new dominion turn starts
    Then the cooldown should be reset
    And the action should be available again

Scenario: Bulk action resolution with batching
    Given 100 actions are queued across a territory
    When the dominion turn processes actions
    Then actions should be batched in groups of 10
    And each batch should request WorldEngine data in parallel
    To minimize HTTP round-trips and latency
```

---

## 9. Error Handling & Edge Cases

### 9.1 Invalid Action Scenarios

```gherkin
Feature: Invalid Action Handling
    As the simulation service
    I want to gracefully handle invalid actions
    So that bad data doesn't crash the service

Scenario: Reject action with invalid persona ID
    Given a queued action with PersonaId "invalid-guid"
    When validating the action
    Then the action should be rejected immediately
    And the rejection reason should be "Invalid PersonaId format"
    And no WorldEngine requests should be made

Scenario: Reject action with invalid settlement ID
    Given an action queued for settlement "non-existent"
    When WorldEngine returns 404 for the settlement
    Then the action should be rejected
    And the rejection reason should be "Settlement not found"

Scenario: Handle deleted persona gracefully
    Given an action is queued for a persona
    And the persona is deleted in WorldEngine before processing
    When the action is processed
    Then the action should fail gracefully
    And no effects should be applied
    And the action should be marked "Cancelled"
```

### 9.2 Concurrency & Race Conditions

```gherkin
Scenario: Handle concurrent influence spending
    Given a persona has 100 influence
    And two actions costing 80 influence each are queued simultaneously
    When both actions attempt to validate concurrently
    Then only one action should succeed
    And the other should be rejected with "Insufficient Influence"
    And optimistic concurrency should prevent double-spending

Scenario: Handle queue limit race condition
    Given a persona has 4 actions queued (limit is 5)
    And two new actions are queued simultaneously
    When both actions check the queue limit
    Then only one action should be queued
    And the other should be rejected with "Queue full"
```

---

## 10. Performance & Scalability

### 10.1 Bulk Processing

```gherkin
Feature: Bulk Action Processing
    As the simulation service
    I want to process actions in bulk
    So that turn processing completes within SLA

Scenario: Process 1000 actions within 5 minutes
    Given 1000 actions are queued across multiple settlements
    When the dominion turn executes
    Then all actions should be processed within 5 minutes
    And throughput should average at least 200 actions/minute

Scenario: Parallel processing by settlement
    Given actions are queued in 10 different settlements
    When processing begins
    Then actions should be processed in parallel per settlement
    And each settlement should have a dedicated worker thread
    To maximize CPU utilization
```

### 10.2 Caching Strategy

```gherkin
Scenario: Cache persona stats to reduce HTTP calls
    Given a persona performs 5 actions in one turn
    When processing the actions
    Then persona stats should be fetched once
    And cached for the duration of the turn
    And cache should be invalidated after turn completes

Scenario: Cache settlement civic stats
    Given 20 actions occur in the same settlement
    When processing the actions
    Then settlement civic stats should be fetched once
    And reused for all 20 actions
```

---

## 11. Observability & Debugging

### 11.1 Logging & Tracing

```gherkin
Feature: Action Processing Observability
    As a developer
    I want detailed logs for action processing
    So that I can debug issues and tune balance

Scenario: Log action success calculation breakdown
    Given an action is processed
    When the success chance is calculated
    Then the log should include:
        | Factor              | ActorValue | Weight | Contribution |
        | IntrigueSkill       | 75         | 0.4    | +30%        |
        | TargetReputation    | 80         | 0.3    | -24%        |
        | SettlementSecurity  | 60         | 0.2    | -12%        |
        | RenownDifference    | +20        | 0.1    | +2%         |
        | Final               | -          | -      | 65%         |

Scenario: Trace WorldEngine requests
    Given an action requires multiple WorldEngine calls
    When processing the action
    Then each HTTP request should be logged with:
        - Request URL and method
        - Request/response payloads
        - Duration in milliseconds
        - Correlation ID for tracing
```

### 11.2 Metrics & Monitoring

```gherkin
Scenario: Track action success rates
    Given actions are processed over time
    When monitoring metrics
    Then the following should be tracked:
        - Action success rate by type (intrigue, diplomacy, etc.)
        - Average processing time per action
        - WorldEngine API latency (p50, p95, p99)
        - Cache hit rate for persona/settlement data

Scenario: Alert on high failure rate
    Given action failure rate exceeds 30% over 1 hour
    When the threshold is breached
    Then a Discord webhook notification should be sent
    And the message should include failure reasons breakdown
```

---

## 12. Test Infrastructure Requirements

### 12.1 Test Fixtures

```gherkin
Feature: BDD Test Infrastructure
    As a developer
    I want reusable test fixtures
    So that BDD scenarios are easy to write and maintain

Background: Shared test setup
    Given the simulation service is running with in-memory database
    And a mock WorldEngine HTTP client is configured
    And the following action definitions are loaded:
        - SpyOnTarget (intrigue, 100 influence, 24h cooldown)
        - BrokerAlliance (diplomacy, 150 influence, 48h cooldown)
        - SabotageTradeRoute (intrigue, 200 influence, 72h cooldown)
    And the mock WorldEngine returns default persona stats:
        | PersonaId | Intrigue | Diplomacy | Renown | Reputation |
        | aaaa...   | 75       | 50        | 120    | 80         |
```

### 12.2 Assertion Helpers

```csharp
// Example BDD step definition structure
[Then(@"the influence balance should be (.*)")]
public void ThenTheInfluenceBalanceShouldBe(int expectedBalance)
{
    var ledger = _scenarioContext.Get<PersonaInfluenceLedger>("Ledger");
    ledger.Balance.Value.Should().Be(expectedBalance);
}

[Then(@"an ""(.*)"" should be published")]
public void ThenAnEventShouldBePublished(string eventType)
{
    var publishedEvents = _scenarioContext.Get<List<ISimulationEvent>>("Events");
    publishedEvents.Should().Contain(e => e.GetType().Name == eventType);
}

[Then(@"GET (.*) should be called")]
public void ThenGetShouldBeCalled(string endpoint)
{
    var mockClient = _scenarioContext.Get<Mock<IWorldEngineClient>>("MockClient");
    mockClient.Verify(c => c.GetAsync(It.Is<string>(url => url.Contains(endpoint))), Times.Once);
}
```

---

## 13. Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Implement `PersonaInfluenceLedger` aggregate
- [ ] Implement `PersonaActionQueue` aggregate
- [ ] Create `PersonaActionDefinition` JSON schema
- [ ] Load action definitions on startup
- [ ] Write BDD specs for influence earning/spending (Sections 1.1, 1.2)

### Phase 2: Action Processing (Week 2)
- [ ] Implement action queue management (Section 2.1)
- [ ] Implement action cooldown tracking (Section 2.2)
- [ ] Implement success calculation engine (Section 3.2)
- [ ] Write BDD specs for queue management and success calculation

### Phase 3: WorldEngine Integration (Week 3)
- [ ] Implement `IWorldEngineClient` interface
- [ ] Create HTTP request/response DTOs
- [ ] Implement data requests for persona stats, reputation, settlement stats (Section 6.1)
- [ ] Implement result submission (Section 6.2)
- [ ] Write BDD specs for integration scenarios

### Phase 4: Action Resolution (Week 4)
- [ ] Implement action resolution logic with effects (Section 5.1)
- [ ] Implement multi-target action support (Section 5.2)
- [ ] Implement settlement context (Section 4.1)
- [ ] Implement reputation/renown modifiers (Section 7)
- [ ] Write BDD specs for resolution scenarios

### Phase 5: Polish & Performance (Week 5)
- [ ] Implement caching strategy (Section 10.2)
- [ ] Implement bulk processing for dominion turns (Section 8.1)
- [ ] Add comprehensive logging and metrics (Section 11)
- [ ] Performance testing and tuning
- [ ] Write BDD specs for error handling (Section 9)

---

## 14. Cross-References

### Related Documents
- `SimulatorRequirements.md` - High-level requirements
- `COMMUNICATION_ARCHITECTURE.md` - HTTP/gRPC integration patterns
- `ARCHITECTURE_ASSESSMENT.md` - Refactoring plan and value objects
- `EconomyGameDesign.md` (WorldEngine) - Persona influence system design
- `PHASE1_COMPLETE.md` - Strongly-typed value objects completed

### Key Value Objects (Already Implemented)
- ✅ `PersonaId` - Strongly-typed persona identifier
- ✅ `SettlementId` - Strongly-typed settlement identifier
- ✅ `InfluenceAmount` - Influence with underflow protection
- ✅ `TurnDate` - Turn timestamp (10-15 minute intervals)
- ✅ `SimulationWorkType` - Discriminated union for work types

### Value Objects Needed
- [ ] `ActionQueueId` - Typed identifier for action queues
- [ ] `ActionDefinitionId` - Typed identifier for action definitions
- [ ] `SuccessFactor` - Record for success calculation factors
- [ ] `ActionCooldown` - Duration-based cooldown with expiry
- [ ] `ReputationScore` - 0-100 score with semantic properties
- [ ] `RenownScore` - Unbounded score with comparison helpers

---

## 15. Success Criteria

✅ All BDD scenarios pass with green tests
✅ No magic strings in action processing logic
✅ WorldEngine integration via typed DTOs only
✅ Comprehensive logging for debugging
✅ Actions process within performance SLA (5 min for 1000 actions)
✅ Circuit breaker prevents cascading failures
✅ Cache hit rate > 80% for repeated data requests
✅ Action success rates tunable via JSON without code changes

---

**Next Steps**:
1. Review this testing path with the team
2. Prioritize Phase 1 scenarios
3. Create Reqnroll feature files from sections 1-2
4. Implement step definitions with mock WorldEngine client
5. Drive implementation through failing tests (TDD/BDD)

**The BDD scenarios are now the requirements. Ship when tests are green!** 🎯

