using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine dialogue trees API.
/// Follows the same patterns as <see cref="InteractionApiService"/>.
/// </summary>
public class DialogueApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string DialogueBase = "/api/worldengine/dialogue";

    public DialogueApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    public Guid? SelectedEndpointId => _selectedEndpointId;

    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    // ==================== CRUD Operations ====================

    public async Task<PagedResult<DialogueTreeDto>> GetAllAsync(
        string? search = null, int page = 1, int pageSize = 50)
    {
        string url = $"{DialogueBase}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";

        return await GetAsync<PagedResult<DialogueTreeDto>>(url) ?? new PagedResult<DialogueTreeDto>();
    }

    public async Task<DialogueTreeDto?> GetByIdAsync(string dialogueTreeId)
    {
        return await GetAsync<DialogueTreeDto>(
            $"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}");
    }

    public async Task<PagedResult<DialogueTreeDto>> GetBySpeakerTagAsync(string speakerTag)
    {
        return await GetAsync<PagedResult<DialogueTreeDto>>(
            $"{DialogueBase}/by-speaker/{Uri.EscapeDataString(speakerTag)}")
               ?? new PagedResult<DialogueTreeDto>();
    }

    public async Task<DialogueTreeDto?> CreateAsync(DialogueTreeDto dto)
    {
        return await PostAsync<DialogueTreeDto>(DialogueBase, dto);
    }

    public async Task<DialogueTreeDto?> UpdateAsync(string dialogueTreeId, DialogueTreeDto dto)
    {
        return await PutAsync<DialogueTreeDto>(
            $"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}", dto);
    }

    public async Task DeleteAsync(string dialogueTreeId)
    {
        await DeleteRequestAsync($"{DialogueBase}/{Uri.EscapeDataString(dialogueTreeId)}");
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
        HttpRequestMessage request = new(method, new Uri(baseUri, relativeUrl));
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
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
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

        throw new WorldEngineApiException(
            (int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }
}
