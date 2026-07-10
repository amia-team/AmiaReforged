using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the WorldEngine area graph API.
/// </summary>
public class AreaGraphApiService : ApiServiceBase
{
    private const string GraphBase = "/api/worldengine/areas/graph";

    public AreaGraphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

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
}
