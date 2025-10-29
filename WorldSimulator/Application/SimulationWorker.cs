using WorldSimulator.Domain.Events;

namespace WorldSimulator.Application;

/// <summary>
/// Main background worker that processes simulation work items.
/// Polls the database for pending work and executes it when the circuit is closed.
/// </summary>
public class SimulationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CircuitBreakerService _circuitBreaker;
    private readonly IEventLogPublisher _eventPublisher;
    private readonly ILogger<SimulationWorker> _logger;
    private readonly IConfiguration _configuration;

    public SimulationWorker(
        IServiceProvider serviceProvider,
        CircuitBreakerService circuitBreaker,
        IEventLogPublisher eventPublisher,
        ILogger<SimulationWorker> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _circuitBreaker = circuitBreaker;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulation Worker starting");

        string environment = _configuration["ENVIRONMENT_NAME"] ?? "Unknown";
        await _eventPublisher.PublishAsync(
            new SimulationServiceStarted(environment),
            EventSeverity.Information);

        int pollIntervalSeconds = _configuration.GetValue<int>("Simulation:PollIntervalSeconds", 5);
        int circuitBreakerWaitSeconds = _configuration.GetValue<int>("Simulation:CircuitBreakerWaitSeconds", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check circuit breaker
                if (!_circuitBreaker.IsAvailable())
                {
                    _logger.LogDebug("Circuit breaker is open, waiting...");
                    await Task.Delay(TimeSpan.FromSeconds(circuitBreakerWaitSeconds), stoppingToken);
                    continue;
                }

                // Process work in a new scope
                await ProcessNextWorkItemAsync(stoppingToken);

                // Wait before next poll
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Simulation Worker stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in simulation worker loop");
                await Task.Delay(TimeSpan.FromSeconds(pollIntervalSeconds), stoppingToken);
            }
        }

        await _eventPublisher.PublishAsync(
            new SimulationServiceStopping("Graceful shutdown"),
            EventSeverity.Information);

        _logger.LogInformation("Simulation Worker stopped");
    }

    private async Task ProcessNextWorkItemAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        SimulationDbContext db = scope.ServiceProvider.GetRequiredService<SimulationDbContext>();

        // Poll for next pending work item
        SimulationWorkItem? workItem = await db.WorkItems
            .Where(w => w.Status == WorkItemStatus.Pending)
            .OrderBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (workItem == null)
        {
            _logger.LogTrace("No pending work items found");
            return;
        }

        _logger.LogInformation("Processing work item {WorkItemId} of type {WorkType}",
            workItem.Id, workItem.WorkType);

        DateTime startTime = DateTime.UtcNow;

        try
        {
            // Mark as processing
            workItem.Start();
            await db.SaveChangesAsync(cancellationToken);

            // Process the work item based on type
            await ProcessWorkItemByTypeAsync(workItem, db, cancellationToken);

            // Mark as completed
            workItem.Complete();
            await db.SaveChangesAsync(cancellationToken);

            TimeSpan duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Completed work item {WorkItemId} in {Duration}ms",
                workItem.Id, duration.TotalMilliseconds);

            await _eventPublisher.PublishAsync(
                new WorkItemCompleted(workItem.Id, workItem.WorkType, duration),
                EventSeverity.Information);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process work item {WorkItemId}", workItem.Id);

            workItem.Fail(ex.Message);
            await db.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                new WorkItemFailed(workItem.Id, workItem.WorkType, ex.Message, workItem.RetryCount),
                EventSeverity.Warning);
        }
    }

    private async Task ProcessWorkItemByTypeAsync(
        SimulationWorkItem workItem,
        SimulationDbContext db,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing work type: {WorkType}", workItem.WorkType);

        // TODO: Implement actual work type handlers
        // For now, this is a placeholder that will be extended with:
        // - DominionTurn
        // - CivicStats
        // - PersonaAction
        // - MarketPricing
        // etc.

        switch (workItem.WorkType)
        {
            case "DominionTurn":
                await ProcessDominionTurnAsync(workItem.Payload, db, cancellationToken);
                break;

            case "CivicStats":
                await ProcessCivicStatsAsync(workItem.Payload, db, cancellationToken);
                break;

            case "PersonaAction":
                await ProcessPersonaActionAsync(workItem.Payload, db, cancellationToken);
                break;

            case "MarketPricing":
                await ProcessMarketPricingAsync(workItem.Payload, db, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown work type: {WorkType}", workItem.WorkType);
                throw new NotImplementedException($"Work type '{workItem.WorkType}' is not implemented");
        }
    }

    // Placeholder methods for work type handlers - to be implemented with TDD/BDD
    private Task ProcessDominionTurnAsync(string payload, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing dominion turn: {Payload}", payload);
        // TODO: Implement dominion turn logic
        return Task.CompletedTask;
    }

    private Task ProcessCivicStatsAsync(string payload, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing civic stats: {Payload}", payload);
        // TODO: Implement civic stats logic
        return Task.CompletedTask;
    }

    private Task ProcessPersonaActionAsync(string payload, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing persona action: {Payload}", payload);
        // TODO: Implement persona action logic
        return Task.CompletedTask;
    }

    private Task ProcessMarketPricingAsync(string payload, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing market pricing: {Payload}", payload);
        // TODO: Implement market pricing logic
        return Task.CompletedTask;
    }
}

