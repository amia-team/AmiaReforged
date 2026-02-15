using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// Service that enforces bans by checking if a player's CD Key is in the bans table
/// when they enter the module. Banned players are immediately booted.
/// </summary>
[ServiceBinding(typeof(BanEnforcementService))]
public class BanEnforcementService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly BanService _banService;

    public BanEnforcementService(BanService banService)
    {
        _banService = banService;
        NwModule.Instance.OnClientEnter += HandleClientEnter;
        Log.Info("BanEnforcementService initialized.");
    }

    private async void HandleClientEnter(ModuleEvents.OnClientEnter eventData)
    {
        try
        {
            NwPlayer player = eventData.Player;
            string cdKey = player.CDKey;

            bool isBanned = await _banService.IsBannedAsync(cdKey);
            await NwTask.SwitchToMainThread();

            if (isBanned)
            {
                Log.Warn($"Banned player attempted to connect: {player.PlayerName} (CD Key: {cdKey})");
                player.BootPlayer("You have been banned from this server.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in BanEnforcementService.HandleClientEnter");
        }
    }
}
