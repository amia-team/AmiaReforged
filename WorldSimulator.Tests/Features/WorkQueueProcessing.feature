Feature: Simulation Work Queue Processing
    As a World Simulator Service
    I want to process queued work items with typed payloads
    So that simulation tasks are executed reliably and type-safely
Background:
    Given the simulation service is running
    And the circuit breaker is closed
Scenario: Process a dominion turn work item successfully
    Given a dominion "Kingdom of Amia" with ID "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    And the dominion has 3 territories, 5 regions, and 8 settlements
    When a dominion turn work item is queued for turn date "2025-10-29"
    Then the work item should be created with status "Pending"
    And the payload should be a valid DominionTurnPayload
    When the simulation worker polls for work
    Then the work item status should transition to "Processing"
    And the dominion turn scenarios should execute in order
    And the work item status should transition to "Completed"
    And a DominionTurnCompleted event should be published
Scenario: Process civic stats calculation
    Given a settlement "Cordor" with ID "12345678-1234-1234-1234-123456789012"
    When a civic stats work item is queued with 30 day lookback period
    Then the work item should be created with status "Pending"
    And the payload should be a valid CivicStatsPayload
    When the simulation worker polls for work
    Then the work item status should transition to "Processing"
    And civic statistics should be aggregated
    And the work item status should transition to "Completed"
    And a SettlementCivicStatsUpdated event should be published
Scenario: Process persona influence action
    Given a persona "Lord Blackwood" with ID "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
    And the persona has 500 influence points
    When a persona action work item is queued for "Intrigue" costing 100 influence
    Then the work item should be created with status "Pending"
    And the payload should be a valid PersonaActionPayload
    When the simulation worker polls for work
    Then the work item status should transition to "Processing"
    And the influence cost should be validated
    And the action should be resolved
    And the work item status should transition to "Completed"
    And a PersonaActionResolved event should be published
Scenario: Reject work item with invalid payload
    Given an invalid dominion turn payload with empty DominionId
    When attempting to create a work item with the invalid payload
    Then a validation exception should be thrown
    And the exception should contain "DominionId cannot be empty"
Scenario: Handle work item processing failure with retry
    Given a civic stats work item is queued
    And the civic stats calculation will fail on first attempt
    When the simulation worker polls for work
    Then the work item status should transition to "Processing"
    And the work item processing should fail
    And the work item status should transition to "Failed"
    And the retry count should be 1
    And the work item should be eligible for retry
    When the work item is requeued
    And the simulation worker polls for work again
    Then the work item status should transition to "Processing"
    And the work item should complete successfully on retry
Scenario: Skip work when circuit breaker is open
    Given a dominion turn work item is queued
    And the WorldEngine health check fails
    And the circuit breaker transitions to "Open"
    When the simulation worker polls for work
    Then the work item should not be processed
    And the work item status should remain "Pending"
    And a CircuitBreakerStateChanged event should be published
Scenario: Process multiple work items in order
    Given the following work items are queued:
        | WorkType      | CreatedAt             | Priority |
        | DominionTurn  | 2025-10-29T09:00:00Z  | High     |
        | CivicStats    | 2025-10-29T10:00:00Z  | Normal   |
        | PersonaAction | 2025-10-29T11:00:00Z  | Normal   |
        | MarketPricing | 2025-10-29T12:00:00Z  | Low      |
    When the simulation worker processes all work
    Then the work items should be processed in creation order:
        | WorkType      |
        | DominionTurn  |
        | CivicStats    |
        | PersonaAction |
        | MarketPricing |
Scenario: Maximum retry limit prevents infinite loops
    Given a persona action work item is queued
    And the action will always fail
    When the work item fails 3 times
    Then the retry count should be 3
    And the work item should not be eligible for retry
    And the work item should remain in "Failed" status
    And a WorkItemFailed event should be published with retry exhausted flag
Scenario: Deserialize typed payload from work item
    Given a market pricing work item with payload:
        | Field              | Value                                    |
        | MarketId           | 99999999-8888-7777-6666-555555555555    |
        | MarketName         | Grand Bazaar                             |
        | RecalculateAllItems| true                                     |
        | EffectiveDate      | 2025-10-29T15:00:00Z                    |
    When the payload is deserialized to MarketPricingPayload
    Then the MarketId should be "99999999-8888-7777-6666-555555555555"
    And the MarketName should be "Grand Bazaar"
    And RecalculateAllItems should be true
    And the EffectiveDate should be "2025-10-29T15:00:00Z"
Scenario: Concurrent work items maintain optimistic concurrency
    Given 2 simulation workers are running
    And a work item is queued
    When both workers attempt to claim the same work item
    Then only one worker should successfully start processing
    And the other worker should receive a concurrency exception
    And the work item version should be incremented only once
