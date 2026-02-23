using System.Net.Http.Json;
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

    private async Task<HttpClient> CreateClientAsync()
    {
        if (_selectedEndpointId == null)
            throw new InvalidOperationException("No WorldEngine endpoint selected. Add one in the Encounters page.");

        var ep = await _endpointService.GetEndpointAsync(_selectedEndpointId.Value);
        if (ep == null)
            throw new InvalidOperationException("The selected WorldEngine endpoint no longer exists.");

        var client = _httpClientFactory.CreateClient("WorldEngine");
        client.BaseAddress = new Uri(ep.BaseUrl.TrimEnd('/') + "/");
        return client;
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
        var http = await CreateClientAsync();
        HttpResponseMessage response = await http.GetAsync(url);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<T?> PostAsync<T>(string url, object? body) where T : class
    {
        var http = await CreateClientAsync();
        HttpResponseMessage response = body != null
            ? await http.PostAsJsonAsync(url, body, JsonOptions)
            : await http.PostAsync(url, null);
        await EnsureSuccessOrThrow(response);

        string content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content)) return null;
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private async Task<T?> PutAsync<T>(string url, object body) where T : class
    {
        var http = await CreateClientAsync();
        HttpResponseMessage response = await http.PutAsJsonAsync(url, body, JsonOptions);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task DeleteAsync(string url)
    {
        var http = await CreateClientAsync();
        HttpResponseMessage response = await http.DeleteAsync(url);
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
