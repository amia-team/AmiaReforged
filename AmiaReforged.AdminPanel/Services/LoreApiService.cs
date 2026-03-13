using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine codex lore definitions API.
/// Follows the same patterns as <see cref="ItemApiService"/>.
/// </summary>
public class LoreApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string LoreBase = "/api/worldengine/codex/lore";

    public LoreApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<LoreDefinitionDto>> GetAllAsync(
        string? search = null, string? category = null, int page = 1, int pageSize = 50)
    {
        string url = $"{LoreBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        return await GetAsync<PagedResult<LoreDefinitionDto>>(url) ?? new PagedResult<LoreDefinitionDto>();
    }

    public async Task<LoreDefinitionDto?> GetByIdAsync(string loreId)
    {
        return await GetAsync<LoreDefinitionDto>($"{LoreBase}/{Uri.EscapeDataString(loreId)}");
    }

    public async Task<LoreDefinitionDto?> CreateAsync(LoreDefinitionDto dto)
    {
        return await PostAsync<LoreDefinitionDto>(LoreBase, dto);
    }

    public async Task<LoreDefinitionDto?> UpdateAsync(string loreId, LoreDefinitionDto dto)
    {
        return await PutAsync<LoreDefinitionDto>($"{LoreBase}/{Uri.EscapeDataString(loreId)}", dto);
    }

    public async Task DeleteAsync(string loreId)
    {
        await DeleteRequestAsync($"{LoreBase}/{Uri.EscapeDataString(loreId)}");
    }

    // ==================== HTTP Helpers ====================

    private async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine endpoint selected.");

        WorldEngineEndpoint? ep = await _endpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException($"Endpoint '{ep.Name}' has no API key configured.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri baseUri, string relativeUrl, string apiKey)
    {
        HttpRequestMessage request = new HttpRequestMessage(method, new Uri(baseUri, relativeUrl));
        request.Headers.Add("X-API-Key", apiKey);
        return request;
    }

    private async Task<T?> GetAsync<T>(string url) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, baseUri, url, apiKey);
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object? body) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, baseUri, url, apiKey);
        if (body != null)
        {
            string json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private async Task<T?> PutAsync<T>(string url, object body) where T : class
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Put, baseUri, url, apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task DeleteRequestAsync(string url)
    {
        (Uri baseUri, string apiKey) = await ResolveEndpointAsync();
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = CreateRequest(HttpMethod.Delete, baseUri, url, apiKey);
        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
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
}
