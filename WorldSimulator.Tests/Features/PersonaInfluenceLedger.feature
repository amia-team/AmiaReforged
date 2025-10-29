Feature: Persona Influence Ledger Management
    As a persona in the game world
    I want to earn and spend influence through actions
    So that I can perform intrigue, diplomacy, and other strategic actions

Background:
    Given the simulation service is running
    And the circuit breaker is closed
    And the WorldEngine mock client is configured

# Earning Influence Scenarios

Scenario: Earn influence from successful civic quest completion
    Given a persona "Lord Blackwood" with PersonaId "aaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
    And the persona has 100 influence points
    When WorldEngine publishes a "CivicQuestCompleted" event granting 50 influence
    Then the persona influence balance should be 150
    And an "InfluenceGrantedEvent" should be published
    And the transaction history should record "Civic Quest: Secure Trade Route"

Scenario: Earn influence from settlement loyalty milestone
    Given a settlement "Cordor" with SettlementId "1111-2222-3333-4444-555555555555"
    And a persona "Mayor Valencia" with PersonaId "bbbb-cccc-dddd-eeee-ffffffffffff" is the elected leader
    And Cordor's loyalty score increases from 60 to 80
    When the civic stats are aggregated
    Then "Mayor Valencia" should earn 25 influence
    And the reason should be "Settlement Loyalty Milestone: Good (80)"
    And an "InfluenceGrantedEvent" should be published

Scenario: Influence grant is idempotent
    Given a persona "Duchess Ravencroft" with PersonaId "cccc-dddd-eeee-ffff-000000000000"
    And the persona has received 50 influence from event "quest-reward-123"
    When the same event "quest-reward-123" is replayed due to message retry
    Then the influence should not be granted again
    And the duplicate should be logged as "Idempotent replay: quest-reward-123"
    And the persona influence balance should remain at 50

# Spending Influence Scenarios

Scenario: Spend influence on successful action
    Given a persona "Duchess Ravencroft" with PersonaId "dddd-eeee-ffff-0000-111111111111"
    And the persona has 500 influence points
    When the persona queues an "Intrigue" action costing 100 influence
    Then the action should be validated successfully
    And the influence balance should be reduced to 400
    And an "InfluenceSpentEvent" should be published
    And the action should be queued for processing

Scenario: Reject action with insufficient influence
    Given a persona "Baron Thorne" with PersonaId "eeee-ffff-0000-1111-222222222222"
    And the persona has 30 influence points
    When the persona attempts to queue a "Diplomacy" action costing 50 influence
    Then the action should be rejected
    And the rejection reason should be "Insufficient Influence: Required 50, Available 30"
    And no "InfluenceSpentEvent" should be published
    And the influence balance should remain 30

Scenario: Partial refund on failed action
    Given a persona "Unlucky Spy" with PersonaId "ffff-0000-1111-2222-333333333333"
    And the persona has 200 influence points
    And the persona spends 100 influence on an intrigue action
    And the action definition specifies "50% refund on failure"
    When the action fails during resolution
    Then 50 influence should be refunded to the persona
    And the persona influence balance should be 150
    And an "InfluenceRefundedEvent" should be published
    And the refund reason should be "Action failed: 50% refund policy"

# Influence Balance Queries

Scenario: Query current influence balance
    Given a persona "Merchant Prince" with PersonaId "0000-1111-2222-3333-444444444444"
    And the persona has earned the following influence:
        | Source                  | Amount |
        | Initial grant           | 100    |
        | Trade deal success      | 75     |
        | Settlement milestone    | 50     |
    And the persona has spent the following influence:
        | Action                  | Cost   |
        | Spy on rival            | 100    |
    When querying the persona's influence balance
    Then the balance should be 125
    And the transaction history should have 4 entries

Scenario: Query influence transaction history
    Given a persona has multiple influence transactions
    When querying the transaction history for the last 30 days
    Then the history should be ordered by timestamp descending
    And each entry should include: source/action, amount, timestamp, balance after

