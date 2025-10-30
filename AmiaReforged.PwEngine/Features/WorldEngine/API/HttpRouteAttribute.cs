using System.Reflection;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Attribute to mark methods as HTTP route handlers.
/// Similar to ASP.NET [HttpGet], [HttpPost], etc.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HttpRouteAttribute : Attribute
{
    public string Method { get; }
    public string Pattern { get; }

    public HttpRouteAttribute(string method, string pattern)
    {
        Method = method ?? throw new ArgumentNullException(nameof(method));
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
    }
}

/// <summary>
/// Convenience attributes for common HTTP methods
/// </summary>
public class HttpGetAttribute : HttpRouteAttribute
{
    public HttpGetAttribute(string pattern) : base("GET", pattern) { }
}

public class HttpPostAttribute : HttpRouteAttribute
{
    public HttpPostAttribute(string pattern) : base("POST", pattern) { }
}

public class HttpPutAttribute : HttpRouteAttribute
{
    public HttpPutAttribute(string pattern) : base("PUT", pattern) { }
}

public class HttpDeleteAttribute : HttpRouteAttribute
{
    public HttpDeleteAttribute(string pattern) : base("DELETE", pattern) { }
}

public class HttpPatchAttribute : HttpRouteAttribute
{
    public HttpPatchAttribute(string pattern) : base("PATCH", pattern) { }
}

