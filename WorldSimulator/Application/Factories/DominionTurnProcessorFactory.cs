using WorldSimulator.Application.Processors;

namespace WorldSimulator.Application.Factories
{
    /// <summary>
    /// Factory for creating DominionTurnProcessor instances with all dependencies.
    /// Avoids "newing up" complex objects and keeps construction logic centralized.
    /// </summary>
    public interface IDominionTurnProcessorFactory
    {
        /// <summary>
        /// Create a processor instance with a specific DbContext scope
        /// </summary>
        IDominionTurnProcessor Create(SimulationDbContext context);
    }

    /// <summary>
    /// Implementation of IDominionTurnProcessorFactory.
    /// Uses constructor injection for dependencies that don't vary per instance.
    /// </summary>
    public class DominionTurnProcessorFactory : IDominionTurnProcessorFactory
    {
        private readonly IEventLogPublisher _eventPublisher;
        private readonly ILoggerFactory _loggerFactory;

        public DominionTurnProcessorFactory(
            IEventLogPublisher eventPublisher,
            ILoggerFactory loggerFactory)
        {
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public IDominionTurnProcessor Create(SimulationDbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ILogger<DominionTurnProcessor> logger = _loggerFactory.CreateLogger<DominionTurnProcessor>();

            return new DominionTurnProcessor(context, _eventPublisher, logger);
        }
    }
}

