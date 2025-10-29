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
        Task PublishAsync(SimulationEvent eventData, EventSeverity severity = EventSeverity.Information, CancellationToken cancellationToken = default);
    }
}

