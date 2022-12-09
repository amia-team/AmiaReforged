using AmiaReforged.System.Services;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ResetMinutesCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string Command => "./resetminutes";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM)
        {
            NWScript.SendMessageToAllDMs(
                $"{caller.PlayerName} tried changing the reset timer of the server and is not a DM.");
            caller.SendServerMessage(
                "You must be a DM to use this command. This incident has been logged for posterity's sake.");
            Log.Warn($"{caller.PlayerName} tried changing the reset time of the server and is not a DM.");
            return Task.CompletedTask;
        }

        if (message.Split(' ').Length <= 1)
        {
            caller.SendServerMessage(
                "./resetminutes usage: \"./resetminutes <number>\" for example, \"./resetminutes 30\"");
            return Task.CompletedTask;
        }

        float newReset = float.Parse(message.Split(' ')[1]);
        NWScript.SetLocalFloat(NwModule.Instance, "minutesToReset", newReset);
        ResetTimeKeeperSingleton.Instance.ResetStartTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        
        NwModule.Instance.SendMessageToAllDMs($"Amia reset timer has been changed to {newReset} minutes");
        return Task.CompletedTask;
    }
}