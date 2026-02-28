using System.Net;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Routes HTTP requests to appropriate handlers
/// </summary>
public interface IApiRouter
{
    Task<ApiResult> RouteAsync(
        string method,
        string path,
        HttpListenerRequest request,
        CancellationToken ct,
        IServiceProvider? serviceProvider = null);
}

