using AmiaReforged.PwEngine.Features.WorldEngine.Application.AreaGraph.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;
using Anvil;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for the area connectivity graph.
/// Provides endpoints to retrieve and refresh the graph of area transitions.
/// </summary>
public class AreaGraphController
{
    /// <summary>
    /// Get the area connectivity graph. Ordinary read dispatches a query through the facade;
    /// forced refresh (?refresh=true) is handled separately (task 009).
    /// GET /api/worldengine/areas/graph?refresh=false
    /// </summary>
    [HttpGet("/api/worldengine/areas/graph")]
    public static async Task<ApiResult> GetGraph(RouteContext ctx)
    {
        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null) return RouteContextExtensions.FacadeUnavailable();

        AreaGraphData graph = await facade.QueryAsync<GetAreaGraphQuery, AreaGraphData>(
            new GetAreaGraphQuery(), ctx.CancellationToken);

        return new ApiResult(200, graph);
    }

    /// <summary>
    /// Force a full rebuild of the area graph from live module data.
    /// POST /api/worldengine/areas/graph/refresh
    /// Explicit refresh is kept separate from the ordinary read query and is
    /// coordinated with task 009 (refresh command/handler).
    /// </summary>
    [HttpPost("/api/worldengine/areas/graph/refresh")]
    public static async Task<ApiResult> RefreshGraph(RouteContext ctx)
    {
        AreaGraphCacheService cache = AnvilCore.GetService<AreaGraphCacheService>()
            ?? throw new InvalidOperationException("AreaGraphCacheService is not available");
        AreaGraphData graph = await cache.RefreshAsync();

        return new ApiResult(200, graph);
    }
}
