using AmiaReforged.PwEngine.Features.DependencyGraph;
using AmiaReforged.PwEngine.Features.WorldEngine.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for the PwEngine dependency graph.
/// Exposes assembly-level type dependency information for visualization.
/// </summary>
public class DependencyGraphController
{
    /// <summary>
    /// Get the full dependency graph for all AmiaReforged assemblies.
    /// GET /api/pwengine/dependencies/graph
    /// Optional query param: ?namespace=AmiaReforged.PwEngine.Features.Crafting
    /// </summary>
    [HttpGet("/api/pwengine/dependencies/graph")]
    public static Task<ApiResult> GetGraph(RouteContext ctx)
    {
        string? namespaceFilter = ctx.GetQueryParam("namespace");

        DependencyGraphDto graph;
        if (!string.IsNullOrWhiteSpace(namespaceFilter))
        {
            graph = DependencyGraphBuilder.GetFilteredGraph(namespaceFilter);
        }
        else
        {
            graph = DependencyGraphBuilder.GetGraph();
        }

        return Task.FromResult(new ApiResult(200, graph));
    }

    /// <summary>
    /// Get just the available namespace list (lightweight endpoint for dropdowns).
    /// GET /api/pwengine/dependencies/namespaces
    /// </summary>
    [HttpGet("/api/pwengine/dependencies/namespaces")]
    public static Task<ApiResult> GetNamespaces(RouteContext ctx)
    {
        DependencyGraphDto graph = DependencyGraphBuilder.GetGraph();

        var namespaces = graph.Namespaces
            .OrderBy(n => n.FullName)
            .Select(n => new { n.FullName, n.Label, n.TypeCount })
            .ToList();

        return Task.FromResult(new ApiResult(200, namespaces));
    }

    /// <summary>
    /// Get summary statistics for the dependency graph.
    /// GET /api/pwengine/dependencies/stats
    /// </summary>
    [HttpGet("/api/pwengine/dependencies/stats")]
    public static Task<ApiResult> GetStats(RouteContext ctx)
    {
        DependencyGraphDto graph = DependencyGraphBuilder.GetGraph();
        return Task.FromResult(new ApiResult(200, graph.Stats));
    }
}
