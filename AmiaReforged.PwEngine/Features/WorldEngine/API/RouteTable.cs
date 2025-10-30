using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Represents a compiled route in the routing table
/// </summary>
internal class CompiledRoute
{
    public string Method { get; }
    public string Pattern { get; }
    public Regex Regex { get; }
    public List<string> ParameterNames { get; }
    public Func<RouteContext, Task<ApiResult>> Handler { get; }
    public string HandlerName { get; }

    public CompiledRoute(
        string method,
        string pattern,
        Regex regex,
        List<string> parameterNames,
        Func<RouteContext, Task<ApiResult>> handler,
        string handlerName)
    {
        Method = method;
        Pattern = pattern;
        Regex = regex;
        ParameterNames = parameterNames;
        Handler = handler;
        HandlerName = handlerName;
    }

    public bool Matches(string method, string path)
    {
        return Method == method && Regex.IsMatch(path);
    }

    public Dictionary<string, string> ExtractRouteValues(string path)
    {
        var match = Regex.Match(path);
        var values = new Dictionary<string, string>();

        for (int i = 0; i < ParameterNames.Count && i < match.Groups.Count - 1; i++)
        {
            values[ParameterNames[i]] = match.Groups[i + 1].Value;
        }

        return values;
    }
}

/// <summary>
/// Route table that discovers and caches routes at startup using reflection.
/// Inspired by ASP.NET Core's routing system but lightweight for Anvil.
/// </summary>
public class RouteTable
{
    private readonly Logger _logger;
    private readonly List<CompiledRoute> _routes;

    public RouteTable(Logger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _routes = new List<CompiledRoute>();
    }

    /// <summary>
    /// Scan an assembly for route handlers and register them
    /// </summary>
    public void ScanAssembly(Assembly assembly)
    {
        _logger.Info("Scanning assembly {Assembly} for route handlers...", assembly.GetName().Name);

        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in types)
        {
            ScanType(type);
        }

        _logger.Info("Route table built with {Count} routes", _routes.Count);
    }

    /// <summary>
    /// Scan a specific type for route handler methods
    /// </summary>
    public void ScanType(Type type)
    {
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attribute = method.GetCustomAttribute<HttpRouteAttribute>();
            if (attribute == null) continue;

            RegisterRoute(attribute.Method, attribute.Pattern, method, type);
        }
    }

    /// <summary>
    /// Manually register a route (for cases where reflection isn't suitable)
    /// </summary>
    public void AddRoute(
        string httpMethod,
        string pattern,
        Func<RouteContext, Task<ApiResult>> handler,
        string handlerName = "Manual")
    {
        var (regex, paramNames) = CompilePattern(pattern);
        var route = new CompiledRoute(httpMethod, pattern, regex, paramNames, handler, handlerName);
        _routes.Add(route);

        _logger.Debug("Registered route: {Method} {Pattern} -> {Handler}",
            httpMethod, pattern, handlerName);
    }

    private void RegisterRoute(string httpMethod, string pattern, MethodInfo method, Type type)
    {
        var (regex, paramNames) = CompilePattern(pattern);

        // Create handler delegate
        Func<RouteContext, Task<ApiResult>> handler;

        if (method.IsStatic)
        {
            // Static method - can invoke directly
            handler = async (ctx) =>
            {
                var result = method.Invoke(null, new object[] { ctx });
                return result is Task<ApiResult> task ? await task : (ApiResult)result!;
            };
        }
        else
        {
            // Instance method - need to create instance
            // Assumption: parameterless constructor exists
            handler = async (ctx) =>
            {
                var instance = Activator.CreateInstance(type);
                var result = method.Invoke(instance, new object[] { ctx });
                return result is Task<ApiResult> task ? await task : (ApiResult)result!;
            };
        }

        var route = new CompiledRoute(
            httpMethod,
            pattern,
            regex,
            paramNames,
            handler,
            $"{type.Name}.{method.Name}");

        _routes.Add(route);

        _logger.Info("Registered route: {Method} {Pattern} -> {Handler}",
            httpMethod, pattern, route.HandlerName);
    }

    /// <summary>
    /// Compile a route pattern into a regex and extract parameter names
    /// Supports patterns like: /api/treasuries/{id}/balance
    /// </summary>
    private (Regex Regex, List<string> ParameterNames) CompilePattern(string pattern)
    {
        var paramNames = new List<string>();

        // Find all {paramName} placeholders
        var paramRegex = new Regex(@"\{([^}]+)\}");
        var matches = paramRegex.Matches(pattern);

        foreach (Match match in matches)
        {
            paramNames.Add(match.Groups[1].Value);
        }

        // Convert pattern to regex
        // /api/treasuries/{id}/balance -> ^/api/treasuries/([^/]+)/balance$
        var regexPattern = "^" + paramRegex.Replace(pattern, @"([^/]+)") + "$";
        var regex = new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return (regex, paramNames);
    }

    /// <summary>
    /// Find a matching route and execute it
    /// </summary>
    public async Task<ApiResult?> DispatchAsync(
        string method,
        string path,
        HttpListenerRequest request,
        CancellationToken ct)
    {
        // Find matching route
        var route = _routes.FirstOrDefault(r => r.Matches(method, path));

        if (route == null)
        {
            _logger.Debug("No route matched: {Method} {Path}", method, path);
            return null;
        }

        _logger.Debug("Route matched: {Method} {Path} -> {Handler}",
            method, path, route.HandlerName);

        // Extract route values
        var routeValues = route.ExtractRouteValues(path);

        // Create context
        var context = new RouteContext(request, routeValues, ct);

        // Execute handler
        try
        {
            return await route.Handler(context);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error executing route handler {Handler}", route.HandlerName);
            throw;
        }
    }

    /// <summary>
    /// Get all registered routes (for debugging/documentation)
    /// </summary>
    public IEnumerable<(string Method, string Pattern, string Handler)> GetRoutes()
    {
        return _routes.Select(r => (r.Method, r.Pattern, r.HandlerName));
    }
}

