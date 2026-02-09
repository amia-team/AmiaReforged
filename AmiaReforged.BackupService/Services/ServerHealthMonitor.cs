using AmiaReforged.BackupService.Configuration;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Continuously monitors server health with regular pings.
/// Provides a cancellation token that gets triggered when the server goes down,
/// allowing in-progress operations to be aborted.
/// </summary>
public interface IServerHealthMonitor
{
    /// <summary>
    /// Whether the server is currently considered available.
    /// </summary>
    bool IsServerAvailable { get; }

    /// <summary>
    /// A cancellation token that gets cancelled when the server becomes unavailable.
    /// This token is recreated when the server becomes available again.
    /// </summary>
    CancellationToken ServerAvailableToken { get; }

    /// <summary>
    /// Event raised when server availability changes.
    /// </summary>
    event EventHandler<ServerAvailabilityChangedEventArgs>? AvailabilityChanged;
}

public class ServerAvailabilityChangedEventArgs : EventArgs
{
    public bool IsAvailable { get; }
    public string? ErrorMessage { get; }

    public ServerAvailabilityChangedEventArgs(bool isAvailable, string? errorMessage = null)
    {
        IsAvailable = isAvailable;
        ErrorMessage = errorMessage;
    }
}

public class ServerHealthMonitor : BackgroundService, IServerHealthMonitor
{
    private readonly ILogger<ServerHealthMonitor> _logger;
    private readonly IServerHealthService _healthService;
    private readonly IDiscordNotificationService _discordService;
    private readonly BackupConfig _config;

    private volatile bool _isServerAvailable = true;
    private CancellationTokenSource _serverAvailableCts = new();
    private readonly object _ctsLock = new();
    private int _consecutiveFailures;
    private string? _lastErrorMessage;

    public bool IsServerAvailable => _isServerAvailable;

    public CancellationToken ServerAvailableToken
    {
        get
        {
            lock (_ctsLock)
            {
                return _serverAvailableCts.Token;
            }
        }
    }

    public event EventHandler<ServerAvailabilityChangedEventArgs>? AvailabilityChanged;

    public ServerHealthMonitor(
        ILogger<ServerHealthMonitor> logger,
        IServerHealthService healthService,
        IDiscordNotificationService discordService,
        BackupConfig config)
    {
        _logger = logger;
        _healthService = healthService;
        _discordService = discordService;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Server Health Monitor starting - pinging every {Interval} seconds",
            _config.HealthPingIntervalSeconds);

        // Do an initial health check
        await CheckHealthAndUpdateStateAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.HealthPingIntervalSeconds), stoppingToken);
                await CheckHealthAndUpdateStateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Server Health Monitor stopping gracefully");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in health monitor loop");
                // Continue monitoring even on errors
            }
        }

        _logger.LogInformation("Server Health Monitor stopped");
    }

    private async Task CheckHealthAndUpdateStateAsync(CancellationToken stoppingToken)
    {
        ServerHealthResult result = await _healthService.CheckHealthAsync(stoppingToken);

        if (result.IsHealthy)
        {
            if (_consecutiveFailures > 0)
            {
                _logger.LogDebug("Health check passed, resetting failure count from {Count}", _consecutiveFailures);
            }

            _consecutiveFailures = 0;

            if (!_isServerAvailable)
            {
                await SetServerAvailableAsync(true, stoppingToken);
            }
        }
        else
        {
            _consecutiveFailures++;
            _lastErrorMessage = result.ErrorMessage;

            _logger.LogWarning("Health check failed ({Count}/{Threshold}): {Error}",
                _consecutiveFailures, _config.ConsecutiveFailuresBeforeUnhealthy, result.ErrorMessage);

            if (_isServerAvailable && _consecutiveFailures >= _config.ConsecutiveFailuresBeforeUnhealthy)
            {
                await SetServerAvailableAsync(false, stoppingToken);
            }
        }
    }

    private async Task SetServerAvailableAsync(bool available, CancellationToken stoppingToken)
    {
        bool previousState = _isServerAvailable;
        _isServerAvailable = available;

        if (available)
        {
            _logger.LogInformation("🟢 Server is now AVAILABLE - backup operations will resume");

            // Create a new CTS for the available state
            lock (_ctsLock)
            {
                _serverAvailableCts = new CancellationTokenSource();
            }

            // Notify via Discord that server recovered
            await _discordService.SendSuccessAsync(
                "🟢 Server Recovered",
                "The server is now reachable again. Backup operations will resume on the next scheduled cycle.",
                stoppingToken);
        }
        else
        {
            _logger.LogWarning("🔴 Server is now UNAVAILABLE - backup operations will be halted");

            // Cancel the current CTS to signal in-progress operations
            lock (_ctsLock)
            {
                _serverAvailableCts.Cancel();
            }

            // Notify via Discord
            await _discordService.SendErrorAsync(
                "🔴 Server Unreachable",
                $"The server has failed {_consecutiveFailures} consecutive health checks and is considered unavailable.\n\n" +
                $"**Last Error:** {_lastErrorMessage}\n\n" +
                "All backup operations are halted until the server recovers.",
                stoppingToken);
        }

        // Raise the event
        AvailabilityChanged?.Invoke(this, new ServerAvailabilityChangedEventArgs(available, _lastErrorMessage));
    }

    public override void Dispose()
    {
        lock (_ctsLock)
        {
            _serverAvailableCts.Dispose();
        }
        base.Dispose();
    }
}
