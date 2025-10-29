namespace WorldSimulator.Domain.Events;

/// <summary>
/// Published when a dominion turn job is started
/// </summary>
public record DominionTurnStartedEvent(
    Guid JobId,
    Guid GovernmentId,
    string GovernmentName,
    DateTime TurnDate
) : SimulationEvent($"Dominion turn started for {GovernmentName}");

/// <summary>
/// Published when a dominion turn job completes successfully
/// </summary>
public record DominionTurnCompletedEvent(
    Guid JobId,
    Guid GovernmentId,
    string GovernmentName,
    DateTime TurnDate,
    int ScenariosProcessed
) : SimulationEvent($"Dominion turn completed for {GovernmentName} - {ScenariosProcessed} scenarios processed");

/// <summary>
/// Published when a dominion turn job fails
/// </summary>
public record DominionTurnFailedEvent(
    Guid JobId,
    Guid GovernmentId,
    string GovernmentName,
    DateTime TurnDate,
    string ErrorMessage
) : SimulationEvent($"Dominion turn failed for {GovernmentName}: {ErrorMessage}");

// New typed events for BDD scenarios
public record DominionTurnCompleted(
    GovernmentId GovernmentId,
    TurnDate TurnDate,
    int ScenariosProcessed,
    TimeSpan ProcessingTime) : SimulationEvent($"Dominion turn completed");

public record SettlementCivicStatsUpdated(
    SettlementId SettlementId,
    CivicScore Loyalty,
    CivicScore Security,
    CivicScore Prosperity,
    DateTimeOffset CalculatedAt) : SimulationEvent($"Civic stats updated");

public record PersonaActionResolved(
    PersonaId PersonaId,
    PersonaActionType ActionType,
    InfluenceAmount CostPaid,
    bool Success,
    string? ResultMessage) : SimulationEvent($"Persona action resolved");

