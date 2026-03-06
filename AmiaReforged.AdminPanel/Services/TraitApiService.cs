using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine trait definitions API.
/// Follows the same patterns as <see cref="LoreApiService"/>.
/// </summary>
public class TraitApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string TraitBase = "/api/worldengine/traits";

    public TraitApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<TraitDefinitionDto>> GetAllAsync(
        string? search = null, string? category = null, string? deathBehavior = null,
        bool? dmOnly = null, int page = 1, int pageSize = 50)
    {
        string url = $"{TraitBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(category))
            url += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrWhiteSpace(deathBehavior))
            url += $"&deathBehavior={Uri.EscapeDataString(deathBehavior)}";
        if (dmOnly.HasValue)
            url += $"&dmOnly={dmOnly.Value}";

        return await GetAsync<PagedResult<TraitDefinitionDto>>(url) ?? new PagedResult<TraitDefinitionDto>();
    }

    public async Task<TraitDefinitionDto?> GetByTagAsync(string tag)
    {
        return await GetAsync<TraitDefinitionDto>($"{TraitBase}/{Uri.EscapeDataString(tag)}");
    }

    public async Task<TraitDefinitionDto?> CreateAsync(TraitDefinitionDto dto)
    {
        return await PostAsync<TraitDefinitionDto>(TraitBase, dto);
    }

    public async Task<TraitDefinitionDto?> UpdateAsync(string tag, TraitDefinitionDto dto)
    {
        return await PutAsync<TraitDefinitionDto>($"{TraitBase}/{Uri.EscapeDataString(tag)}", dto);
    }

    public async Task DeleteAsync(string tag)
    {
        await DeleteRequestAsync($"{TraitBase}/{Uri.EscapeDataString(tag)}");
    }

    // ==================== Enum Lookups ====================

    public async Task<List<EnumValueDto>> GetCategoriesAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/categories") ?? [];
    }

    public async Task<List<EnumValueDto>> GetDeathBehaviorsAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/death-behaviors") ?? [];
    }

    public async Task<List<EnumValueDto>> GetEffectTypesAsync()
    {
        return await GetAsync<List<EnumValueDto>>($"{TraitBase}/effect-types") ?? [];
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
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        var response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private async Task<T?> PutAsync<T>(string url, object body) where T : class
    {
        var (baseUri, apiKey) = await ResolveEndpointAsync();
        var http = _httpClientFactory.CreateClient("WorldEngine");
        using var request = CreateRequest(HttpMethod.Put, baseUri, url, apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task DeleteRequestAsync(string url)
    {
        var (baseUri, apiKey) = await ResolveEndpointAsync();
        var http = _httpClientFactory.CreateClient("WorldEngine");
        using var request = CreateRequest(HttpMethod.Delete, baseUri, url, apiKey);
        var response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
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
