namespace WorldSimulator.Application.Processors
{
    /// <summary>
    /// Processes dominion turn jobs by executing scenarios in hierarchy order.
    /// Application service implementing the domain interface.
    /// </summary>
    public class DominionTurnProcessor : IDominionTurnProcessor
    {
        private readonly SimulationDbContext _context;
        private readonly IEventLogPublisher _eventPublisher;
        private readonly ILogger<DominionTurnProcessor> _logger;

        public DominionTurnProcessor(
            SimulationDbContext context,
            IEventLogPublisher eventPublisher,
            ILogger<DominionTurnProcessor> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(DominionTurnJob job, CancellationToken cancellationToken = default)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            try
            {
                _logger.LogInformation(
                    "Processing dominion turn for {GovernmentName} ({GovernmentId}) - {TotalScenarios} scenarios",
                    job.GovernmentName, job.GovernmentId, job.TotalScenarios);

                job.Start();
                await _context.SaveChangesAsync(cancellationToken);

                await PublishEventAsync(new DominionTurnStartedEvent(
                    job.Id,
                    job.GovernmentId,
                    job.GovernmentName,
                    job.TurnDate), cancellationToken);

                // Execute scenarios in order
                await ExecuteTerritoryScenarios(job, cancellationToken);
                await ExecuteRegionScenarios(job, cancellationToken);
                await ExecuteSettlementScenarios(job, cancellationToken);

                job.Complete();
                await _context.SaveChangesAsync(cancellationToken);

                await PublishEventAsync(new DominionTurnCompletedEvent(
                    job.Id,
                    job.GovernmentId,
                    job.GovernmentName,
                    job.TurnDate,
                    job.ScenariosProcessed), cancellationToken);

                _logger.LogInformation(
                    "Completed dominion turn for {GovernmentName} - processed {ScenariosProcessed} scenarios",
                    job.GovernmentName, job.ScenariosProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process dominion turn for {GovernmentName}: {ErrorMessage}",
                    job.GovernmentName, ex.Message);

                job.Fail(ex.Message);
                await _context.SaveChangesAsync(cancellationToken);

                await PublishEventAsync(new DominionTurnFailedEvent(
                    job.Id,
                    job.GovernmentId,
                    job.GovernmentName,
                    job.TurnDate,
                    ex.Message), cancellationToken);

                throw;
            }
        }

        private async Task ExecuteTerritoryScenarios(DominionTurnJob job, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing territory scenarios for {GovernmentName}", job.GovernmentName);

            // TODO: Load territories from WorldEngine database
            // TODO: Execute territory-level economic calculations
            // TODO: Process resource allocation

            job.RecordScenarioCompleted();
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ExecuteRegionScenarios(DominionTurnJob job, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing region scenarios for {GovernmentName}", job.GovernmentName);

            // TODO: Load regions from WorldEngine database
            // TODO: Execute region-level trade calculations
            // TODO: Process inter-settlement commerce

            job.RecordScenarioCompleted();
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task ExecuteSettlementScenarios(DominionTurnJob job, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing settlement scenarios for {GovernmentName}", job.GovernmentName);

            // TODO: Load settlements from WorldEngine database
            // TODO: Update civic statistics
            // TODO: Process market pricing
            // TODO: Execute persona actions

            job.RecordScenarioCompleted();
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task PublishEventAsync(SimulationEvent evt, CancellationToken cancellationToken)
        {
            await _eventPublisher.PublishAsync(evt, EventSeverity.Information, cancellationToken);
        }
    }
}

namespace WorldSimulator.Domain.Services
{
    /// <summary>
    /// Processes dominion turn jobs, orchestrating the hierarchy execution.
    /// Domain service that coordinates Territory → Region → Settlement scenarios.
    /// </summary>
    public interface IDominionTurnProcessor
    {
        /// <summary>
        /// Processes a dominion turn job through all its scenarios
        /// </summary>
        /// <param name="job">The job to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        Task ProcessAsync(DominionTurnJob job, CancellationToken cancellationToken = default);
    }
}

