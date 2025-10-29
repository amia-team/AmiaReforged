using Polly;
using Polly.CircuitBreaker;
using WorldSimulator.Domain.Events;

namespace WorldSimulator.Infrastructure.Services;

/// <summary>
/// Circuit breaker service that monitors WorldEngine availability.
/// Pauses simulation work when WorldEngine is unavailable.
/// </summary>
public class CircuitBreakerService : IHostedService, IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly IEventLogPublisher _eventPublisher;
    private readonly IConfiguration _configuration;
    private Timer? _healthCheckTimer;
    private Domain.ValueObjects.CircuitState _state = Domain.ValueObjects.CircuitState.Closed;
    private readonly object _stateLock = new();
    private HttpClient? _httpClient;

    public CircuitBreakerService(
        IHttpClientFactory httpClientFactory,
        ILogger<CircuitBreakerService> logger,
        IEventLogPublisher eventPublisher,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _configuration = configuration;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Circuit Breaker Service");

        _httpClient = _httpClientFactory.CreateClient("WorldEngine");

        int intervalSeconds = _configuration.GetValue<int>("CircuitBreaker:CheckIntervalSeconds", 30);
        _healthCheckTimer = new Timer(
            async _ => await CheckWorldEngineHealthAsync(),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(intervalSeconds));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Circuit Breaker Service");
        _healthCheckTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async Task CheckWorldEngineHealthAsync()
    {
        string host = _configuration["WorldEngine:Host"] ?? "http://localhost:8080";
        string endpoint = _configuration["WorldEngine:HealthEndpoint"] ?? "/health";
        int timeout = _configuration.GetValue<int>("CircuitBreaker:TimeoutSeconds", 5);

        try
        {
            using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            HttpResponseMessage response = await _httpClient!.GetAsync($"{host}{endpoint}", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                await TransitionToClosedAsync(host);
            }
            else
            {
                await TransitionToOpenAsync(host, $"Health check returned {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await TransitionToOpenAsync(host, ex.Message);
        }
    }

    private async Task TransitionToClosedAsync(string host)
    {
        lock (_stateLock)
        {
            if (_state == Domain.ValueObjects.CircuitState.Closed)
                return;

            _state = Domain.ValueObjects.CircuitState.Closed;
        }

        _logger.LogInformation("Circuit breaker CLOSED - WorldEngine available at {Host}", host);

        await _eventPublisher.PublishAsync(
            new CircuitBreakerStateChanged("Closed", host, null),
            EventSeverity.Information);
    }

    private async Task TransitionToOpenAsync(string host, string error)
    {
        lock (_stateLock)
        {
            if (_state == Domain.ValueObjects.CircuitState.Open)
                return;

            _state = Domain.ValueObjects.CircuitState.Open;
        }

        _logger.LogWarning("Circuit breaker OPEN - WorldEngine unavailable at {Host}: {Error}", host, error);

        await _eventPublisher.PublishAsync(
            new CircuitBreakerStateChanged("Open", host, error),
            EventSeverity.Critical);
    }

    /// <summary>
    /// Checks if the circuit is closed and WorldEngine is available
    /// </summary>
    public virtual bool IsAvailable()
    {
        lock (_stateLock)
        {
            return _state == Domain.ValueObjects.CircuitState.Closed;
        }
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
        _httpClient?.Dispose();
    }
}

