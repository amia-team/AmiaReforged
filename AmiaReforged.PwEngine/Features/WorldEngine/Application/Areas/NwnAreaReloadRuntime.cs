using System.Linq;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Areas;

/// <summary>
/// NWN-specific implementation of <see cref="IAreaReloadRuntime"/>.
/// Owns all live NWN access: main-thread switching, area lookup, occupancy checks,
/// destruction, recreation, and DM notifications.
/// </summary>
[ServiceBinding(typeof(IAreaReloadRuntime))]
public sealed class NwnAreaReloadRuntime : IAreaReloadRuntime
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public async Task<AreaReloadResult> ReloadAreaAsync(string resRef)
    {
        // Area operations must run on the main NWN thread.
        await NwTask.SwitchToMainThread();

        NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a =>
            a.ResRef.Equals(resRef, StringComparison.OrdinalIgnoreCase));

        if (area == null)
        {
            Log.Warn("Area reload: resref \"{ResRef}\" not found.", resRef);
            return AreaReloadResult.NotFound(resRef);
        }

        // Ensure no players are in the area before destroying it.
        int playerCount = area.Objects.OfType<NwCreature>().Count(c => c.IsPlayerControlled(out _));

        if (playerCount > 0)
        {
            Log.Warn("Area reload: area \"{AreaName}\" has {PlayerCount} player(s), aborting.", area.Name, playerCount);
            return AreaReloadResult.Occupied(resRef, area.Name, playerCount);
        }

        string areaName = area.Name;

        area.Destroy();
        Log.Info("Area \"{AreaName}\" (resref: {ResRef}) destroyed for reload.", areaName, resRef);

        NwArea? recreated = NwArea.Create(resRef);

        if (recreated != null)
        {
            Log.Info("Area \"{AreaName}\" (resref: {ResRef}) recreated successfully.", areaName, resRef);
            NotifyAllDMs($"Area \"{areaName}\" was reloaded via the Admin Panel.");
            return AreaReloadResult.Reloaded(resRef, areaName);
        }

        Log.Error("Failed to recreate area \"{AreaName}\" (resref: {ResRef}).", areaName, resRef);
        NotifyAllDMs($"WARNING: Area \"{areaName}\" was destroyed but could not be recreated via Admin Panel!");
        return AreaReloadResult.RecreateFailed(resRef, areaName);
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
