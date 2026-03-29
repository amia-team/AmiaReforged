using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Result of an area reload API call.
/// </summary>
public sealed record AreaReloadResult(bool Success, string Message, string? Detail = null);

/// <summary>
/// HTTP client wrapper for the WorldEngine area reload API.
/// Calls the NWN server to destroy and recreate an area by resref.
/// </summary>
public class AreaReloadApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;
    private readonly ILogger<AreaReloadApiService> _logger;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string ReloadBase = "/api/worldengine/areas/reload";

    public AreaReloadApiService(
        IHttpClientFactory httpClientFactory,
        IWorldEngineEndpointService endpointService,
        ILogger<AreaReloadApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
        _logger = logger;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    /// <summary>
    /// Returns the available WorldEngine server endpoints for the UI dropdown.
    /// </summary>
    public async Task<IReadOnlyList<WorldEngineEndpoint>> GetEndpointsAsync()
    {
        return await _endpointService.GetAllEndpointsAsync();
    }

    /// <summary>
    /// Send a reload command to the selected NWN server for the given resref.
    /// </summary>
    public async Task<AreaReloadResult> ReloadAreaAsync(string resref)
    {
        if (string.IsNullOrWhiteSpace(resref))
        {
            return new AreaReloadResult(false, "Resref is required.");
        }

        try
        {
            (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
            HttpClient http = _httpClientFactory.CreateClient("WorldEngine");

            string url = $"{ReloadBase}/{Uri.EscapeDataString(resref)}";
            using HttpRequestMessage request = new(HttpMethod.Post, new Uri(baseUri, url));
            request.Headers.Add("X-API-Key", apiKey);

            _logger.LogInformation("Sending area reload for resref '{Resref}' to {BaseUri}", resref, baseUri);

            HttpResponseMessage response = await http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Area reload succeeded for resref '{Resref}'", resref);

                try
                {
                    using JsonDocument doc = JsonDocument.Parse(body);
                    string message = doc.RootElement.TryGetProperty("message", out JsonElement msgEl)
                        ? msgEl.GetString() ?? "Reload succeeded."
                        : "Reload succeeded.";
                    return new AreaReloadResult(true, message);
                }
                catch
                {
                    return new AreaReloadResult(true, "Area reloaded successfully.");
                }
            }
            else
            {
                _logger.LogWarning("Area reload failed for resref '{Resref}': {StatusCode} {Body}",
                    resref, (int)response.StatusCode, body);

                try
                {
                    ApiErrorResponse? error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
                    if (error != null)
                    {
                        return new AreaReloadResult(false, error.Error, error.Detail);
                    }
                }
                catch (JsonException) { }

                return new AreaReloadResult(false, $"Server returned {(int)response.StatusCode}", body);
            }
        }
        catch (InvalidOperationException ex)
        {
            return new AreaReloadResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reloading area '{Resref}'", resref);
            return new AreaReloadResult(false, "Unexpected error", ex.Message);
        }
    }

    private async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine server endpoint selected. Select a server first.");

        WorldEngineEndpoint? ep = await _endpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException($"Endpoint '{ep.Name}' has no API key configured.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }
}
