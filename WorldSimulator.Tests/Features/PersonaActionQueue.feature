Feature: Persona Action Queue Management
    As a persona
    I want to queue multiple actions for processing
    But be limited to prevent spam and maintain game balance

Background:
    Given the simulation service is running
    And the circuit breaker is closed
    And the following action definitions are loaded:
        | ActionId         | ActionType | InfluenceCost | CooldownHours |
        | spy-on-target    | Intrigue   | 100           | 24            |
        | broker-alliance  | Diplomacy  | 150           | 48            |
        | sabotage-route   | Intrigue   | 200           | 72            |
    And the queue limit is 5 actions per persona

# Queue Management Scenarios

Scenario: Queue action within limit
    Given a persona "Spymaster Vex" with PersonaId "aaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
    And the persona has 500 influence points
    And the persona has an empty action queue
    When the persona queues 3 "spy-on-target" actions targeting different personas
    Then all 3 actions should be queued successfully
    And the queue count should be 3
    And the persona influence balance should be 200
    And 3 "InfluenceSpentEvent" events should be published

Scenario: Reject action when queue is full
    Given a persona "Busy Diplomat" with PersonaId "bbbb-cccc-dddd-eeee-ffffffffffff"
    And the persona has 1000 influence points
    And the persona has 5 actions already queued (at limit)
    When the persona attempts to queue a "broker-alliance" action
    Then the action should be rejected
    And the rejection reason should be "Action queue full: 5/5"
    And no influence should be spent
    And no "InfluenceSpentEvent" should be published

Scenario: Queue slot freed after action processes
    Given a persona "Strategic Planner" with PersonaId "cccc-dddd-eeee-ffff-000000000000"
    And the persona has 5 actions queued (at limit)
    When one action completes processing
    Then the queue count should be 4
    And the persona can queue a new action successfully

Scenario: Actions process in FIFO order
    Given a persona "Meticulous Schemer" with PersonaId "dddd-eeee-ffff-0000-111111111111"
    And the persona has 1000 influence points
    And the persona queues actions in order:
        | ActionId         | QueuedAt            |
        | spy-on-target    | 2025-10-29T14:00:00Z|
        | broker-alliance  | 2025-10-29T14:05:00Z|
        | sabotage-route   | 2025-10-29T14:10:00Z|
    When the simulation worker processes the queue
    Then actions should resolve in order: spy-on-target, broker-alliance, sabotage-route
    And the timestamps should reflect FIFO processing

# Action Cooldown Scenarios

Scenario: Action cooldown prevents spam
    Given a persona "Impatient Spy" with PersonaId "eeee-ffff-0000-1111-222222222222"
    And the persona successfully completed "spy-on-target" action at "2025-10-29T14:00:00Z"
    And the "spy-on-target" action has a 24-hour cooldown
    When the persona attempts the same action "spy-on-target" at "2025-10-30T02:00:00Z"
    Then the action should be rejected
    And the rejection reason should contain "Action on cooldown: 12h remaining"
    And no influence should be spent

Scenario: Action cooldown expires and action is available
    Given a persona "Patient Operative" with PersonaId "ffff-0000-1111-2222-333333333333"
    And the persona completed "spy-on-target" action at "2025-10-29T14:00:00Z"
    And the "spy-on-target" action has a 24-hour cooldown
    When the persona attempts "spy-on-target" at "2025-10-30T14:30:00Z"
    Then the action should be queued successfully

Scenario: Different actions don't share cooldowns
    Given a persona "Multi-Talented Agent" with PersonaId "0000-1111-2222-3333-444444444444"
    And the persona completed "spy-on-target" at "2025-10-29T14:00:00Z"
    And "spy-on-target" has a 24-hour cooldown
    When the persona queues "sabotage-route" at "2025-10-29T15:00:00Z"
    Then the action should be queued successfully

Scenario: Per-turn cooldown resets at turn boundary
    Given a persona "Dominion Strategist" with PersonaId "1111-2222-3333-4444-555555555555"
    And an action "diplomatic-pressure" has a "per-turn" cooldown
    And the action was used in turn "2025-10-29T14:00:00Z"
    When a new dominion turn starts at "2025-10-29T14:15:00Z"
    Then the cooldown should be reset
    And the action should be available again

# Queue Priority and Cancellation

Scenario: Cancel queued action and refund influence
    Given a persona "Indecisive Noble" with PersonaId "2222-3333-4444-5555-666666666666"
    And the persona has queued a "broker-alliance" action costing 150 influence
    And the action is still pending (not yet processed)
    When the persona cancels the queued action
    Then the action should be removed from the queue
    And 150 influence should be refunded
    And an "InfluenceRefundedEvent" should be published
    And the refund reason should be "Action cancelled by user"

Scenario: Cannot cancel action that is already processing
    Given a persona has a queued action
    And the action has started processing (status = "Processing")
    When the persona attempts to cancel the action
    Then the cancellation should be rejected
    And the rejection reason should be "Cannot cancel action in progress"

Scenario: High-priority action jumps queue (special ability)
    Given a persona "Government Leader" with PersonaId "3333-4444-5555-6666-777777777777"
    And the persona has the "priority-queue" ability
    And the persona has 3 normal actions queued
    When the persona queues a high-priority "diplomatic-emergency" action
    Then the action should be inserted at the front of the queue
    And it should process next before the existing 3 actions

