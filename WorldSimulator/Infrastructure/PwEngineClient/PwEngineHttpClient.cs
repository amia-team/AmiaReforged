using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace WorldSimulator.Infrastructure.PwEngineClient;

/// <summary>
/// HTTP client for PwEngine WorldEngine API with resilience policies
/// </summary>
public class PwEngineHttpClient : IPwEngineClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PwEngineHttpClient> _logger;
    private readonly ResiliencePipeline<HttpResponseMessage> _resiliencePipeline;

    public PwEngineHttpClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PwEngineHttpClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PwEngine");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Create resilience pipeline with configuration
        int maxRetries = configuration.GetValue("PwEngine:Retry:MaxAttempts", 3);
        int failuresBeforeBreaking = configuration.GetValue("PwEngine:CircuitBreaker:FailuresBeforeOpening", 5);
        int breakDuration = configuration.GetValue("PwEngine:CircuitBreaker:DurationOfBreakSeconds", 30);

        _resiliencePipeline = PwEngineResiliencePolicies.CreatePipeline(
            maxRetries,
            failuresBeforeBreaking,
            breakDuration);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Checking PwEngine health...");

            // Execute with resilience pipeline
            HttpResponseMessage response = await _resiliencePipeline.ExecuteAsync(
                async token => await _httpClient.GetAsync("health", token),
                ct);

            if (response.IsSuccessStatusCode)
            {
                HealthResponse? health = await response.Content.ReadFromJsonAsync<HealthResponse>(ct);
                _logger.LogInformation("PwEngine health check successful: {Status}", health?.Status);
                return true;
            }

            _logger.LogWarning("PwEngine health check failed with status {StatusCode}",
                response.StatusCode);
            return false;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("PwEngine circuit breaker is OPEN - service unavailable");
            return false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "PwEngine health check failed - HTTP error");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "PwEngine health check timed out");
            return false;
        }
    }

    private record HealthResponse(string Status, string Service, DateTime Timestamp);
}

