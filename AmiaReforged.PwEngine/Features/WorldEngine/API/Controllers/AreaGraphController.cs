using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for the area connectivity graph.
/// Provides endpoints to retrieve and refresh the graph of area transitions.
/// </summary>
public class AreaGraphController
{
    private static AreaGraphCacheService? _cacheService;

    /// <summary>
    /// Get the area connectivity graph. Returns cached version unless ?refresh=true is specified.
    /// GET /api/worldengine/areas/graph?refresh=false
    /// </summary>
    [HttpGet("/api/worldengine/areas/graph")]
    public static async Task<ApiResult> GetGraph(RouteContext ctx)
    {
        var cache = ResolveCacheService();

        bool refresh = string.Equals(ctx.GetQueryParam("refresh"), "true", StringComparison.OrdinalIgnoreCase);
        var graph = cache.GetOrBuild(forceRefresh: refresh);

        return await Task.FromResult(new ApiResult(200, graph));
    }

    /// <summary>
    /// Force a full rebuild of the area graph from live module data.
    /// POST /api/worldengine/areas/graph/refresh
    /// </summary>
    [HttpPost("/api/worldengine/areas/graph/refresh")]
    public static async Task<ApiResult> RefreshGraph(RouteContext ctx)
    {
        var cache = ResolveCacheService();
        var graph = cache.Refresh();

        return await Task.FromResult(new ApiResult(200, graph));
    }

    private static AreaGraphCacheService ResolveCacheService()
    {
        if (_cacheService != null) return _cacheService;

        // Lazy-initialize: the builder doesn't need Anvil DI since it uses NwModule.Instance directly
        var builder = new AreaGraphBuilder();
        _cacheService = new AreaGraphCacheService(builder);
        return _cacheService;
    }
}
