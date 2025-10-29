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
            EventSeverity.Information, stoppingToken);

        int pollIntervalSeconds = _configuration.GetValue("Simulation:PollIntervalSeconds", 5);
        int circuitBreakerWaitSeconds = _configuration.GetValue("Simulation:CircuitBreakerWaitSeconds", 30);

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
            EventSeverity.Information, stoppingToken);

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
            workItem.Id, workItem.WorkType.GetType().Name);

        DateTime startTime = DateTime.UtcNow;

        try
        {
            // Mark as processing
            workItem.Start();
            await db.SaveChangesAsync(cancellationToken);

            // Process the work item based on type using pattern matching
            await ProcessWorkItemByTypeAsync(workItem, db, cancellationToken);

            // Mark as completed
            workItem.Complete();
            await db.SaveChangesAsync(cancellationToken);

            TimeSpan duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Completed work item {WorkItemId} in {Duration}ms",
                workItem.Id, duration.TotalMilliseconds);

            await _eventPublisher.PublishAsync(
                new WorkItemCompleted(workItem.Id, workItem.WorkType.GetType().Name, duration),
                EventSeverity.Information, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process work item {WorkItemId}", workItem.Id);

            workItem.Fail(ex.Message);
            await db.SaveChangesAsync(cancellationToken);

            await _eventPublisher.PublishAsync(
                new WorkItemFailed(workItem.Id, workItem.WorkType.GetType().Name, ex.Message, workItem.RetryCount),
                EventSeverity.Warning, cancellationToken);
        }
    }

    private async Task ProcessWorkItemByTypeAsync(
        SimulationWorkItem workItem,
        SimulationDbContext db,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing work type: {WorkType}", workItem.WorkType.GetType().Name);

        // Pattern match on strongly-typed SimulationWorkType
        // Compiler ensures exhaustiveness checking!
        var result = workItem.WorkType switch
        {
            SimulationWorkType.DominionTurn dt => ProcessDominionTurnAsync(dt, db, cancellationToken),
            SimulationWorkType.CivicStatsAggregation cs => ProcessCivicStatsAsync(cs, db, cancellationToken),
            SimulationWorkType.PersonaAction pa => ProcessPersonaActionAsync(pa, db, cancellationToken),
            SimulationWorkType.MarketPricing mp => ProcessMarketPricingAsync(mp, db, cancellationToken),
            _ => throw new NotImplementedException($"Work type '{workItem.WorkType.GetType().Name}' is not implemented")
        };

        await result;
    }

    // Strongly-typed work type handlers - to be implemented with TDD/BDD
    private Task ProcessDominionTurnAsync(SimulationWorkType.DominionTurn command, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing dominion turn for government {GovernmentId} on {TurnDate}",
            command.GovernmentId, command.TurnDate);

        // TODO: Implement dominion turn logic
        // - Request territory/region/settlement data from WorldEngine via HTTP/gRPC
        // - Execute economic calculations locally
        // - Send results back to WorldEngine via HTTP POST/event
        return Task.CompletedTask;
    }

    private Task ProcessCivicStatsAsync(SimulationWorkType.CivicStatsAggregation command, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing civic stats for settlement {SettlementId} since {SinceTimestamp}",
            command.SettlementId, command.SinceTimestamp);

        // TODO: Implement civic stats logic
        // - Request event history from WorldEngine via HTTP/gRPC
        // - Aggregate civic statistics locally (loyalty, security, prosperity, etc.)
        // - Send updated stats back to WorldEngine via HTTP POST/event
        return Task.CompletedTask;
    }

    private Task ProcessPersonaActionAsync(SimulationWorkType.PersonaAction command, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing persona action for {PersonaId}: {ActionType} costing {Cost} influence",
            command.PersonaId, command.ActionType, command.Cost);

        // TODO: Implement persona action logic
        // - Request influence ledger from WorldEngine via HTTP/gRPC
        // - Validate and process action locally
        // - Send action results back to WorldEngine via HTTP POST/event
        return Task.CompletedTask;
    }

    private Task ProcessMarketPricingAsync(SimulationWorkType.MarketPricing command, SimulationDbContext db, CancellationToken ct)
    {
        _logger.LogDebug("Processing market pricing for market {MarketId}, item {ItemId} with demand signal {DemandSignal}",
            command.MarketId, command.ItemId, command.DemandSignal);

        // TODO: Implement market pricing logic
        // - Request demand/supply data from WorldEngine via HTTP/gRPC
        // - Calculate pricing adjustments locally
        // - Send price updates back to WorldEngine via HTTP POST/event
        return Task.CompletedTask;
    }
}

