using System.Net;
using System.Text.Json;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Context passed to route handlers containing request information and utilities
/// </summary>
public class RouteContext
{
    public HttpListenerRequest? Request { get; }
    public Dictionary<string, string> RouteValues { get; }
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Service provider for resolving dependencies in controllers.
    /// Set by the HTTP server when dispatching requests.
    /// </summary>
    public IServiceProvider? Services { get; set; }

    public RouteContext(
        HttpListenerRequest? request,
        Dictionary<string, string> routeValues,
        CancellationToken cancellationToken)
    {
        Request = request;
        RouteValues = routeValues;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Get a route parameter value by name
    /// </summary>
    public string GetRouteValue(string key)
    {
        return RouteValues.TryGetValue(key, out string? value) ? value : string.Empty;
    }

    /// <summary>
    /// Get a query string parameter
    /// </summary>
    public string? GetQueryParam(string key)
    {
        return Request?.QueryString[key];
    }

    /// <summary>
    /// Read and deserialize JSON body
    /// </summary>
    public async Task<T?> ReadJsonBodyAsync<T>()
    {
        if (Request == null) return default;

        using StreamReader reader = new StreamReader(Request.InputStream);
        string json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(json);
    }
}

