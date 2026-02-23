using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine encounter API.
/// Endpoints are managed at runtime via <see cref="IWorldEngineEndpointService"/>
/// and persisted to JSON on disk.
/// </summary>
public class EncounterApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;

    /// <summary>Currently selected endpoint id (null = none selected).</summary>
    private Guid? _selectedEndpointId;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private const string ProfilesBase = "/api/worldengine/encounters/profiles";
    private const string GroupsBase = "/api/worldengine/encounters/groups";
    private const string BonusesBase = "/api/worldengine/encounters/bonuses";
    private const string CacheBase = "/api/worldengine/encounters/cache";

    public EncounterApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
    }

    /// <summary>The id of the currently selected endpoint, or null.</summary>
    public Guid? SelectedEndpointId => _selectedEndpointId;

    /// <summary>Select a WorldEngine endpoint by id. Pass null to deselect.</summary>
    public void SelectEndpoint(Guid? endpointId) => _selectedEndpointId = endpointId;

    /// <summary>
    /// Resolves the selected endpoint and returns its base URI and API key.
    /// Throws if no endpoint is selected, the endpoint no longer exists, or has no API key.
    /// </summary>
    private async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine endpoint selected. Add one in the Encounters page.");

        var ep = await _endpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException(
                $"Endpoint '{ep.Name}' has no API key configured. Edit the endpoint and add one.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }

    /// <summary>Creates an <see cref="HttpRequestMessage"/> with the API key attached.</summary>
    private static HttpRequestMessage CreateRequest(HttpMethod method, Uri baseUri, string relativeUrl, string apiKey)
    {
        var request = new HttpRequestMessage(method, new Uri(baseUri, relativeUrl));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return request;
    }

    // ==================== Profiles ====================

    public async Task<List<SpawnProfileDto>> GetAllProfilesAsync()
    {
        return await GetAsync<List<SpawnProfileDto>>(ProfilesBase) ?? [];
    }

    public async Task<SpawnProfileDto?> GetProfileAsync(Guid id)
    {
        return await GetAsync<SpawnProfileDto>($"{ProfilesBase}/{id}");
    }

    public async Task<SpawnProfileDto?> GetProfileByAreaAsync(string areaResRef)
    {
        return await GetAsync<SpawnProfileDto>($"{ProfilesBase}/by-area/{areaResRef}");
    }

    public async Task<SpawnProfileDto?> CreateProfileAsync(CreateProfileRequest request)
    {
        return await PostAsync<SpawnProfileDto>(ProfilesBase, request);
    }

    public async Task<SpawnProfileDto?> UpdateProfileAsync(Guid id, UpdateProfileRequest request)
    {
        return await PutAsync<SpawnProfileDto>($"{ProfilesBase}/{id}", request);
    }

    public async Task DeleteProfileAsync(Guid id)
    {
        await DeleteAsync($"{ProfilesBase}/{id}");
    }

    public async Task ActivateProfileAsync(Guid id)
    {
        await PostAsync<object>($"{ProfilesBase}/{id}/activate", null);
    }

    public async Task DeactivateProfileAsync(Guid id)
    {
        await PostAsync<object>($"{ProfilesBase}/{id}/deactivate", null);
    }

    // ==================== Groups ====================

    public async Task<SpawnGroupDto?> AddGroupAsync(Guid profileId, CreateGroupRequest request)
    {
        return await PostAsync<SpawnGroupDto>($"{ProfilesBase}/{profileId}/groups", request);
    }

    public async Task<SpawnGroupDto?> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request)
    {
        return await PutAsync<SpawnGroupDto>($"{GroupsBase}/{groupId}", request);
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        await DeleteAsync($"{GroupsBase}/{groupId}");
    }

    // ==================== Bonuses ====================

    public async Task<SpawnBonusDto?> AddBonusAsync(Guid profileId, CreateBonusRequest request)
    {
        return await PostAsync<SpawnBonusDto>($"{ProfilesBase}/{profileId}/bonuses", request);
    }

    public async Task<SpawnBonusDto?> UpdateBonusAsync(Guid bonusId, UpdateBonusRequest request)
    {
        return await PutAsync<SpawnBonusDto>($"{BonusesBase}/{bonusId}", request);
    }

    public async Task DeleteBonusAsync(Guid bonusId)
    {
        await DeleteAsync($"{BonusesBase}/{bonusId}");
    }

    // ==================== Cache ====================

    public async Task RefreshCacheAsync()
    {
        await PostAsync<object>($"{CacheBase}/refresh", null);
    }

    // ==================== HTTP Helpers ====================

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
                throw new EncounterApiException((int)response.StatusCode, error.Error, error.Detail);
        }
        catch (JsonException)
        {
            // Not a structured error response
        }

        throw new EncounterApiException((int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }
}

/// <summary>
/// Exception thrown when the WorldEngine API returns an error.
/// </summary>
public class EncounterApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorTitle { get; }
    public string Detail { get; }

    public EncounterApiException(int statusCode, string errorTitle, string detail)
        : base($"[{statusCode}] {errorTitle}: {detail}")
    {
        StatusCode = statusCode;
        ErrorTitle = errorTitle;
        Detail = detail;
    }
}
