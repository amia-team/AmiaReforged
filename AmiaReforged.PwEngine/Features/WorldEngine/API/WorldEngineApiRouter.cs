using System.Net;
using System.Reflection;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Router with reflection-based route discovery and caching.
/// Scans for [HttpGet], [HttpPost], etc. attributes at startup.
/// </summary>
public class WorldEngineApiRouter : IApiRouter
{
    private readonly Logger _logger;
    private readonly RouteTable _routeTable;

    public WorldEngineApiRouter(Logger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _routeTable = new RouteTable(logger);

        BuildRouteTable();
    }

    private void BuildRouteTable()
    {
        _logger.Info("Building route table...");

        // Scan current assembly for controllers with route attributes
        Assembly assembly = Assembly.GetExecutingAssembly();
        _routeTable.ScanAssembly(assembly);

        // Log all registered routes
        _logger.Info("Route table complete. Registered routes:");
        foreach ((string method, string pattern, string handler) in _routeTable.GetRoutes())
        {
            _logger.Info("  {Method} {Pattern} -> {Handler}", method, pattern, handler);
        }
    }

    public async Task<ApiResult> RouteAsync(
        string method,
        string path,
        HttpListenerRequest request,
        CancellationToken ct,
        IServiceProvider? serviceProvider = null)
    {
        _logger.Debug("Routing {Method} {Path}", method, path);

        // Try to dispatch using route table
        ApiResult? result = await _routeTable.DispatchAsync(method, path, request, ct, serviceProvider);

        if (result != null)
        {
            return result;
        }

        // No route matched
        _logger.Warn("No route matched for {Method} {Path}", method, path);
        return new ApiResult(404, new ErrorResponse("Not found", $"No handler for {method} {path}"));
    }

    /// <summary>
    /// Manually add a route (for special cases)
    /// </summary>
    public void AddRoute(
        string method,
        string pattern,
        Func<RouteContext, Task<ApiResult>> handler,
        string name = "Manual")
    {
        _routeTable.AddRoute(method, pattern, handler, name);
    }
}

