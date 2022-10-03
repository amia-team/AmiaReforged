using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ServerPanel : IChatCommand
{
    public string Command { get; } = "./serverpanel";

    public void ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM || !caller.IsPlayerDM)
        {
            NWScript.SendMessageToAllDMs(
                $"{caller.PlayerName} tried to access the server control panel and is not a DM.");
            caller.SendServerMessage(
                "You must be a DM to use this command. This incident has been logged for posterity's sake.");
            return;
        }
        caller.SendServerMessage("This command is not yet supported.", Color.FromRGBA("#8b0000"));
    }
}