namespace WorldSimulator.Domain.Events
{
    /// <summary>
    /// Base class for all simulation events
    /// </summary>
    public abstract record SimulationEvent(string Message)
    {
        /// <summary>When the event occurred</summary>
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Published when circuit breaker state changes
    /// </summary>
    public record CircuitBreakerStateChanged(string State, string Host, string? Error)
        : SimulationEvent($"Circuit breaker state changed to {State} for {Host}");

    /// <summary>
    /// Published when a work item completes successfully
    /// </summary>
    public record WorkItemCompleted(Guid Id, string Type, TimeSpan Duration)
        : SimulationEvent($"Work item {Type} ({Id}) completed in {Duration.TotalSeconds:F2}s");

    /// <summary>
    /// Published when a work item fails
    /// </summary>
    public record WorkItemFailed(Guid Id, string Type, string Error, int RetryCount)
        : SimulationEvent($"Work item {Type} ({Id}) failed: {Error} (retry {RetryCount})");

    /// <summary>
    /// Published when a work item is queued
    /// </summary>
    public record WorkItemQueued(Guid Id, string Type)
        : SimulationEvent($"Work item {Type} ({Id}) queued");

    /// <summary>
    /// Published when simulation service starts
    /// </summary>
    public record SimulationServiceStarted(string Environment)
        : SimulationEvent($"Simulation service started in {Environment} environment");

    /// <summary>
    /// Published when simulation service stops
    /// </summary>
    public record SimulationServiceStopping(string Reason)
        : SimulationEvent($"Simulation service stopping: {Reason}");
}

