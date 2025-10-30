namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// Example controller showing route handler patterns.
/// Methods marked with [HttpGet], [HttpPost], etc. are auto-discovered.
/// </summary>
public class HealthController
{
    /// <summary>
    /// Health check endpoint
    /// GET /api/worldengine/health
    /// </summary>
    [HttpGet("/api/worldengine/health")]
    public static async Task<ApiResult> GetHealth(RouteContext ctx)
    {
        return await Task.FromResult(new ApiResult(200, new
        {
            status = "healthy",
            service = "WorldEngine",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        }));
    }

    /// <summary>
    /// Detailed health check with dependencies
    /// GET /api/worldengine/health/detailed
    /// </summary>
    [HttpGet("/api/worldengine/health/detailed")]
    public static async Task<ApiResult> GetDetailedHealth(RouteContext ctx)
    {
        // In real implementation, check database, services, etc.
        return await Task.FromResult(new ApiResult(200, new
        {
            status = "healthy",
            service = "WorldEngine",
            timestamp = DateTime.UtcNow,
            dependencies = new
            {
                database = "healthy",
                eventBus = "healthy"
            }
        }));
    }
}

