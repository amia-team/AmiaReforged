namespace WorldSimulator.Domain.Services
{
    /// <summary>
    /// Service for publishing simulation events to external systems (Discord, logs, etc.)
    /// </summary>
    public interface IEventLogPublisher
    {
        /// <summary>
        /// Publishes a simulation event asynchronously
        /// </summary>
        Task PublishAsync(SimulationEvent eventData, EventSeverity severity = EventSeverity.Info);
    }
}

namespace WorldSimulator.Domain.Events
{
    /// <summary>
    /// Base class for all simulation events
    /// </summary>
    public abstract record SimulationEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Published when circuit breaker state changes
    /// </summary>
    public record CircuitBreakerStateChanged : SimulationEvent
    {
        public required string State { get; init; }
        public required string Host { get; init; }
        public string? Error { get; init; }
    }

    /// <summary>
    /// Published when a work item completes successfully
    /// </summary>
    public record WorkItemCompleted : SimulationEvent
    {
        public required Guid Id { get; init; }
        public required string Type { get; init; }
        public TimeSpan Duration { get; init; }
    }

    /// <summary>
    /// Published when a work item fails
    /// </summary>
    public record WorkItemFailed : SimulationEvent
    {
        public required Guid Id { get; init; }
        public required string Type { get; init; }
        public required string Error { get; init; }
        public int RetryCount { get; init; }
    }

    /// <summary>
    /// Published when a work item is queued
    /// </summary>
    public record WorkItemQueued : SimulationEvent
    {
        public required Guid Id { get; init; }
        public required string Type { get; init; }
    }

    /// <summary>
    /// Published when simulation service starts
    /// </summary>
    public record SimulationServiceStarted : SimulationEvent
    {
        public required string Environment { get; init; }
    }

    /// <summary>
    /// Published when simulation service stops
    /// </summary>
    public record SimulationServiceStopping : SimulationEvent
    {
        public required string Reason { get; init; }
    }
}

