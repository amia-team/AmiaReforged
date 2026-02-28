using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine area graph API.
/// </summary>
public class AreaGraphApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string GraphBase = "/api/worldengine/areas/graph";

    public AreaGraphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    /// <summary>
    /// Get the area graph. If <paramref name="refresh"/> is true, forces a rebuild on the server.
    /// </summary>
    public async Task<AreaGraphDto?> GetGraphAsync(bool refresh = false)
    {
        string url = refresh ? $"{GraphBase}?refresh=true" : GraphBase;
        return await GetAsync<AreaGraphDto>(url);
    }

    /// <summary>
    /// Force a full rebuild of the area graph on the server.
    /// </summary>
    public async Task<AreaGraphDto?> RefreshGraphAsync()
    {
        return await PostAsync<AreaGraphDto>($"{GraphBase}/refresh", null);
    }

    // ==================== HTTP Helpers ====================

    private async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine endpoint selected.");

        var ep = await _endpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException($"Endpoint '{ep.Name}' has no API key configured.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri baseUri, string relativeUrl, string apiKey)
    {
        var request = new HttpRequestMessage(method, new Uri(baseUri, relativeUrl));
        request.Headers.Add("X-API-Key", apiKey);
        return request;
    }

    private async Task<T?> GetAsync<T>(string url) where T : class
    {
        var (baseUri, apiKey) = await ResolveEndpointAsync();
        var http = _httpClientFactory.CreateClient("WorldEngine");
        using var request = CreateRequest(HttpMethod.Get, baseUri, url, apiKey);
        var response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object? body) where T : class
    {
        var (baseUri, apiKey) = await ResolveEndpointAsync();
        var http = _httpClientFactory.CreateClient("WorldEngine");
        using var request = CreateRequest(HttpMethod.Post, baseUri, url, apiKey);
        if (body != null)
        {
            string json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }
        var response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        string body = await response.Content.ReadAsStringAsync();
        try
        {
            var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (error != null)
                throw new WorldEngineApiException((int)response.StatusCode, error.Error, error.Detail);
        }
        catch (JsonException) { }

        throw new WorldEngineApiException((int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }
}
