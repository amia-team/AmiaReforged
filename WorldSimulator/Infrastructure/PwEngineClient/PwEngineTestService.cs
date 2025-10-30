using System.Net.Http.Json;

namespace WorldSimulator.Infrastructure.PwEngineClient;

/// <summary>
/// Service for testing and verifying PwEngine connectivity
/// Sends Hello requests to verify communication
/// </summary>
public class PwEngineTestService
{
    private readonly IPwEngineClient _pwEngineClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PwEngineTestService> _logger;
    private readonly IConfiguration _configuration;

    public PwEngineTestService(
        IPwEngineClient pwEngineClient,
        IHttpClientFactory httpClientFactory,
        ILogger<PwEngineTestService> logger,
        IConfiguration configuration)
    {
        _pwEngineClient = pwEngineClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Send a Hello request to PwEngine and log the response
    /// Non-blocking - doesn't fail if PwEngine isn't ready
    /// </summary>
    public async Task<bool> SendHelloToPwEngineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string baseUrl = _configuration["PwEngine:BaseUrl"]
                           ?? "http://localhost:8080/api/worldengine/";

            using var httpClient = _httpClientFactory.CreateClient("PwEngine");

            var payload = new
            {
                message = $"Hello from WorldSimulator at {DateTime.UtcNow:O}"
            };

            _logger.LogInformation("Sending Hello request to PwEngine at {BaseUrl}echo/hello", baseUrl);

            var response = await httpClient.PostAsJsonAsync(
                $"{baseUrl}echo/hello",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("PwEngine Echo Response: {Response}", content);
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "PwEngine Echo returned non-success status {StatusCode}: {ReasonPhrase}",
                    response.StatusCode,
                    response.ReasonPhrase);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            // Check if it's a connection refused (normal on startup)
            if (ex.InnerException is System.Net.Sockets.SocketException socketEx &&
                socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
            {
                // Log at Info level - visible but not alarming
                _logger.LogInformation("PwEngine not yet available for Hello request (will retry via CircuitBreaker)");
                return false;
            }

            // Other HTTP errors - log as warning
            _logger.LogWarning(ex, "Failed to send Hello to PwEngine (will retry via CircuitBreaker)");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending Hello to PwEngine");
            return false;
        }
    }

    /// <summary>
    /// Send a Ping request to verify connectivity
    /// Non-blocking - doesn't fail if PwEngine isn't ready yet
    /// </summary>
    public async Task<bool> PingPwEngineAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string baseUrl = _configuration["PwEngine:BaseUrl"]
                           ?? "http://localhost:8080/api/worldengine/";

            using var httpClient = _httpClientFactory.CreateClient("PwEngine");

            _logger.LogInformation("Pinging PwEngine at {BaseUrl}echo/ping", baseUrl);

            var response = await httpClient.GetAsync(
                $"{baseUrl}echo/ping",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("PwEngine Ping Response: {Response}", content);
                return true;
            }
            else
            {
                _logger.LogWarning(
                    "PwEngine Ping returned non-success status {StatusCode}",
                    response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            // Check if it's a connection refused (normal on startup)
            if (ex.InnerException is System.Net.Sockets.SocketException socketEx &&
                socketEx.SocketErrorCode == System.Net.Sockets.SocketError.ConnectionRefused)
            {
                // Log at Info level - visible but not alarming
                _logger.LogInformation("PwEngine not yet available (will retry via CircuitBreaker)");
                return false;
            }

            // Other HTTP errors - log as warning
            _logger.LogWarning(ex, "Failed to ping PwEngine (will retry via CircuitBreaker)");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error pinging PwEngine");
            return false;
        }
    }
}


