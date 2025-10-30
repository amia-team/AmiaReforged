namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// Echo controller for testing inter-service communication.
/// Used by WorldSimulator to verify connectivity.
/// </summary>
public class EchoController
{
    /// <summary>
    /// Echo endpoint - receives a message and echoes it back with metadata
    /// POST /api/worldengine/echo/hello
    /// </summary>
    [HttpPost("/api/worldengine/echo/hello")]
    public static async Task<ApiResult> EchoHello(RouteContext ctx)
    {
        try
        {
            // Try to read request body
            var request = await ctx.ReadJsonBodyAsync<HelloRequest>();

            if (request == null)
            {
                return new ApiResult(400, new
                {
                    error = "Bad Request",
                    message = "Request body is required",
                    expectedFormat = new { message = "string" }
                });
            }

            // Echo back with metadata
            return new ApiResult(200, new
            {
                received = request.Message,
                echoed = request.Message,
                receivedAt = DateTime.UtcNow,
                service = "PwEngine",
                status = "acknowledged"
            });
        }
        catch (Exception ex)
        {
            return new ApiResult(500, new
            {
                error = "Internal Server Error",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Simple GET ping endpoint
    /// GET /api/worldengine/echo/ping
    /// </summary>
    [HttpGet("/api/worldengine/echo/ping")]
    public static async Task<ApiResult> EchoPing(RouteContext ctx)
    {
        return await Task.FromResult(new ApiResult(200, new
        {
            pong = true,
            receivedAt = DateTime.UtcNow,
            service = "PwEngine"
        }));
    }
}

/// <summary>
/// Request model for hello endpoint
/// </summary>
public class HelloRequest
{
    public required string Message { get; set; }
}

