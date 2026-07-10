using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// HTTP client wrapper for the PwEngine dependency graph API.
/// </summary>
public class DependencyGraphApiService : ApiServiceBase
{
    private const string GraphBase = "/api/pwengine/dependencies";

    public DependencyGraphApiService(IHttpClientFactory httpClientFactory, IWorldEngineEndpointService endpointService)
        : base(httpClientFactory, endpointService)
    {
    }

    /// <summary>
    /// Get the full dependency graph, optionally filtered by namespace prefix.
    /// </summary>
    public async Task<DependencyGraphDto?> GetGraphAsync(string? namespaceFilter = null)
    {
        string url = $"{GraphBase}/graph";
        if (!string.IsNullOrWhiteSpace(namespaceFilter))
        {
            url += $"?namespace={Uri.EscapeDataString(namespaceFilter)}";
        }

        return await GetAsync<DependencyGraphDto>(url);
    }

    /// <summary>
    /// Get summary statistics for the dependency graph.
    /// </summary>
    public async Task<DependencyGraphStats?> GetStatsAsync()
    {
        return await GetAsync<DependencyGraphStats>($"{GraphBase}/stats");
    }
}
