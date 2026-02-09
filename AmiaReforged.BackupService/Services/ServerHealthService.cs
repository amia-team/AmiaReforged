using AmiaReforged.BackupService.Configuration;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Result of a server health check.
/// </summary>
public record ServerHealthResult(bool IsHealthy, string? ErrorMessage = null)
{
    public static ServerHealthResult Healthy() => new(true);
    public static ServerHealthResult Unhealthy(string error) => new(false, error);
}

/// <summary>
/// Service for checking server health before performing backups.
/// </summary>
public interface IServerHealthService
{
    /// <summary>
    /// Checks if the server is healthy and reachable.
    /// </summary>
    Task<ServerHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

public class ServerHealthService : IServerHealthService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServerHealthService> _logger;
    private readonly BackupConfig _config;

    public ServerHealthService(
        IHttpClientFactory httpClientFactory,
        ILogger<ServerHealthService> logger,
        BackupConfig config)
    {
        _httpClient = httpClientFactory.CreateClient("ServerHealth");
        _logger = logger;
        _config = config;
    }

    public async Task<ServerHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        string endpoint = _config.GetServerHealthEndpoint();

        if (string.IsNullOrEmpty(endpoint))
        {
            _logger.LogWarning("Server health endpoint not configured, skipping health check");
            return ServerHealthResult.Healthy(); // Assume healthy if not configured
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.HealthCheckTimeoutSeconds));

            _logger.LogDebug("Checking server health at {Endpoint}", endpoint);

            // Create request with API key header if configured
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

            string? apiKey = _config.GetServerApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Add("X-API-Key", apiKey);
            }
            else
            {
                _logger.LogDebug("No API key configured for server health check");
            }

            HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Server health check successful");
                return ServerHealthResult.Healthy();
            }

            string error = $"Server returned status code {(int)response.StatusCode} ({response.StatusCode})";
            _logger.LogWarning("Server health check failed: {Error}", error);
            return ServerHealthResult.Unhealthy(error);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            string error = $"Server health check timed out after {_config.HealthCheckTimeoutSeconds} seconds";
            _logger.LogWarning(error);
            return ServerHealthResult.Unhealthy(error);
        }
        catch (HttpRequestException ex)
        {
            string error = $"Server unreachable: {ex.Message}";
            _logger.LogWarning(ex, "Server health check failed - HTTP error");
            return ServerHealthResult.Unhealthy(error);
        }
        catch (Exception ex)
        {
            string error = $"Health check error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error during server health check");
            return ServerHealthResult.Unhealthy(error);
        }
    }
}
