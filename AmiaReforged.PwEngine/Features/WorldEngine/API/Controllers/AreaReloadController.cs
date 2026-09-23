using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using NLog;

using AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;


namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for reloading areas by resref.
/// Delegates the actual destroy/recreate work to the WorldEngine command pipeline
/// (via <see cref="IWorldEngineFacade"/>), which routes to <c>ReloadAreaCommand</c>
/// and its runtime handler. This controller performs no NWN work itself.
/// </summary>
public class AreaReloadController
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Reload an area by destroying and recreating it from the module resource.
    /// POST /api/worldengine/areas/reload/{resref}
    ///
    /// Fails if the area has players in it or if the resref is not found.
    /// </summary>
    [HttpPost("/api/worldengine/areas/reload/{resref}")]
    public static async Task<ApiResult> ReloadArea(RouteContext ctx)
    {
        string resRef = ctx.GetRouteValue("resref");

        if (string.IsNullOrWhiteSpace(resRef))
        {
            return new ApiResult(400, new ErrorResponse("Missing resref", "A resref route parameter is required."));
        }

        IWorldEngineFacade? facade = ctx.ResolveFacade();
        if (facade is null)
        {
            return RouteContextExtensions.FacadeUnavailable();
        }

        CommandResult result = await facade.ExecuteAsync(
            new ReloadAreaCommand { ResRef = resRef },
            ctx.CancellationToken);

        return MapResult(result, resRef);
    }

    private static ApiResult MapResult(CommandResult result, string resRef)
    {
        string outcome = result.Data?["outcome"] as string ?? string.Empty;

        if (result.Success && outcome == "reloaded")
        {
            string name = result.Data?["name"] as string ?? string.Empty;
            string message = result.Data?["message"] as string ?? $"Area \"{name}\" reloaded successfully.";
            Log.Info("Area \"{AreaName}\" (resref: {ResRef}) reloaded via API.", name, resRef);

            return new ApiResult(200, new
            {
                resref = resRef,
                name,
                status = "reloaded",
                message
            });
        }

        string detail = result.Data?["message"] as string ?? result.ErrorMessage ?? "Command failed.";

        return outcome switch
        {
            "area_not_found" => new ApiResult(404, new ErrorResponse("Area not found", detail)),
            "area_occupied" => new ApiResult(409, new ErrorResponse("Area occupied", detail)),
            "recreate_failed" => new ApiResult(500, new ErrorResponse("Recreate failed", detail)),
            _ => new ApiResult(400, new ErrorResponse("Command failed", detail)),
        };
    }
}
