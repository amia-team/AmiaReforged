using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine Glyph visual scripting API.
/// Follows the same endpoint-selection pattern as <see cref="EncounterApiService"/>.
/// </summary>
public class GlyphApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string DefinitionsBase = "/api/worldengine/glyphs";
    private const string NodeCatalogPath = "/api/worldengine/glyphs/node-catalog";
    private const string BindingsBase = "/api/worldengine/glyphs/bindings";

    public GlyphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    // ==================== Definitions ====================

    public async Task<List<GlyphDefinitionDto>> GetAllDefinitionsAsync()
    {
        return await GetAsync<List<GlyphDefinitionDto>>(DefinitionsBase) ?? [];
    }

    public async Task<GlyphDefinitionDto?> GetDefinitionAsync(Guid id)
    {
        return await GetAsync<GlyphDefinitionDto>($"{DefinitionsBase}/{id}");
    }

    public async Task<GlyphDefinitionDto?> CreateDefinitionAsync(CreateGlyphRequest request)
    {
        return await PostAsync<GlyphDefinitionDto>(DefinitionsBase, request);
    }

    public async Task<GlyphDefinitionDto?> UpdateDefinitionAsync(Guid id, UpdateGlyphRequest request)
    {
        return await PutAsync<GlyphDefinitionDto>($"{DefinitionsBase}/{id}", request);
    }

    public async Task DeleteDefinitionAsync(Guid id)
    {
        await DeleteAsync($"{DefinitionsBase}/{id}");
    }

    // ==================== Node Catalog ====================

    public async Task<List<GlyphNodeCatalogEntryDto>> GetNodeCatalogAsync()
    {
        return await GetAsync<List<GlyphNodeCatalogEntryDto>>(NodeCatalogPath) ?? [];
    }

    // ==================== Bindings ====================

    public async Task<List<GlyphBindingDto>> GetBindingsForProfileAsync(Guid profileId)
    {
        return await GetAsync<List<GlyphBindingDto>>($"{BindingsBase}?profileId={profileId}") ?? [];
    }

    public async Task<List<GlyphBindingDto>> GetAllBindingsAsync()
    {
        return await GetAsync<List<GlyphBindingDto>>(BindingsBase) ?? [];
    }

    public async Task<GlyphBindingDto?> CreateBindingAsync(CreateGlyphBindingRequest request)
    {
        return await PostAsync<GlyphBindingDto>(BindingsBase, request);
    }

    public async Task DeleteBindingAsync(Guid id)
    {
        await DeleteAsync($"{BindingsBase}/{id}");
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
            throw new InvalidOperationException(
                $"Endpoint '{ep.Name}' has no API key configured.");

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
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
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

    private async Task DeleteAsync(string url)
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
            ApiErrorResponse? error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (error != null)
                throw new WorldEngineApiException((int)response.StatusCode, error.Error, error.Detail);
        }
        catch (JsonException)
        {
            // Not a structured error response
        }

        throw new WorldEngineApiException(
            (int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }
}
