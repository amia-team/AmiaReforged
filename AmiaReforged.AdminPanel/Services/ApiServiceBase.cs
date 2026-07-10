using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Shared HTTP plumbing for all WorldEngine API services.
/// Subclasses keep only endpoint-specific business methods.
/// </summary>
public abstract class ApiServiceBase
{
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly IWorldEngineEndpointService EndpointService;
    private Guid? _selectedEndpointId;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    protected ApiServiceBase(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        HttpClientFactory = httpClientFactory;
        EndpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    // ==================== HTTP Helpers ====================

    protected async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine endpoint selected.");

        WorldEngineEndpoint? ep = await EndpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException($"Endpoint '{ep.Name}' has no API key configured.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }

    protected static HttpRequestMessage CreateRequest(HttpMethod method, Uri baseUri, string relativeUrl, string apiKey)
    {
        HttpRequestMessage request = new(method, new Uri(baseUri, relativeUrl));
        request.Headers.Add("X-API-Key", apiKey);
        return request;
    }

    protected async Task<T?> GetAsync<T>(string url) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = HttpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, baseUri, url, apiKey);
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected async Task<T?> PostAsync<T>(string url, object? body, bool rawJson = false) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = HttpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, baseUri, url, apiKey);
        if (body != null)
        {
            string json = rawJson && body is string s ? s : JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    protected async Task<T?> PutAsync<T>(string url, object body) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = HttpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Put, baseUri, url, apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    protected async Task DeleteRequestAsync(string url)
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = HttpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, baseUri, url, apiKey);
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
    }

    protected virtual async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        string body = await response.Content.ReadAsStringAsync();
        try
        {
            ApiErrorResponse? error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (error != null)
                throw new WorldEngineApiException((int)response.StatusCode, error.Error, error.Detail);
        }
        catch (JsonException) { }

        throw new WorldEngineApiException((int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }

    /// <summary>
    /// Fetches a raw JSON export endpoint and returns pretty-printed JSON.
    /// Used by Item, Region, Industry, and Workstation export endpoints.
    /// </summary>
    protected async Task<string> FetchExportJsonAsync(string url)
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = HttpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, baseUri, url, apiKey);
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string raw = await response.Content.ReadAsStringAsync();
        try
        {
            JsonElement parsed = JsonSerializer.Deserialize<JsonElement>(raw);
            return JsonSerializer.Serialize(parsed, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return raw;
        }
    }
}
