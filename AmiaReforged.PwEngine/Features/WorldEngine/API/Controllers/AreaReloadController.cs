using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API.Controllers;

/// <summary>
/// REST API controller for reloading areas by resref.
/// Destroys and recreates an area from the module resource, mirroring the DM chat command.
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

        // Area operations must run on the main NWN thread
        await NwTask.SwitchToMainThread();

        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a =>
            a.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase));

        if (area == null)
        {
            Log.Warn("Area reload via API: resref \"{ResRef}\" not found.", resRef);
            return new ApiResult(404, new ErrorResponse("Area not found", $"No area found with resref \"{resRef}\"."));
        }

        // Ensure no players are in the area
        int playerCount = area.Objects.OfType<NwCreature>().Count(c => c.IsPlayerControlled(out _));

        if (playerCount > 0)
        {
            Log.Warn("Area reload via API: area \"{AreaName}\" has {PlayerCount} player(s), aborting.", area.Name, playerCount);
            return new ApiResult(409, new ErrorResponse("Area occupied",
                $"Cannot reload \"{area.Name}\" — {playerCount} player(s) still in the area."));
        }

        string areaName = area.Name;

        area.Destroy();
        Log.Info("Area \"{AreaName}\" (resref: {ResRef}) destroyed for API reload.", areaName, resRef);

        NwArea? recreated = NwArea.Create(resRef);

        if (recreated != null)
        {
            Log.Info("Area \"{AreaName}\" (resref: {ResRef}) recreated successfully via API.", areaName, resRef);

            // Notify all DMs in-game
            NotifyAllDMs($"Area \"{areaName}\" was reloaded via the Admin Panel.");

            return new ApiResult(200, new
            {
                resref = resRef,
                name = areaName,
                status = "reloaded",
                message = $"Area \"{areaName}\" reloaded successfully."
            });
        }
        else
        {
            Log.Error("Failed to recreate area \"{AreaName}\" (resref: {ResRef}) via API.", areaName, resRef);

            NotifyAllDMs($"WARNING: Area \"{areaName}\" was destroyed but could not be recreated via Admin Panel!");

            return new ApiResult(500, new ErrorResponse("Recreate failed",
                $"Destroyed \"{areaName}\" but failed to recreate it. The resref may be invalid."));
        }
    }

    private static void NotifyAllDMs(string message)
    {
        foreach (NwPlayer player in NwModule.Instance.Players)
        {
            if (player.IsDM || player.IsPlayerDM)
            {
                player.SendServerMessage(message, ColorConstants.Cyan);
            }
        }
    }
}
