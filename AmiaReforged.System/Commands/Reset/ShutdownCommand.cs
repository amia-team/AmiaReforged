using AmiaReforged.System.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class Shutdown : IChatCommand
{
    private readonly SchedulerService _schedulerService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public string Command => "./shutdown";

    public Shutdown(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
    }

    public void ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM || !caller.IsPlayerDM)
        {
            NWScript.SendMessageToAllDMs(
                $"{caller.PlayerName} tried shutting down the server and is not a DM.");
            caller.SendServerMessage(
                "You must be a DM to use this command. This incident has been logged for posterity's sake.");
            Log.Warn($"{caller.PlayerName} tried shutting down the server and is not a DM.");
            return;
        }

        ShutdownManager shutdownManager = new(_schedulerService);
        shutdownManager.InitiateShutdown();
    }
}