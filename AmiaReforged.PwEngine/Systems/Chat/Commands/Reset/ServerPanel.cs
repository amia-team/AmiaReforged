﻿using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ServerPanel : IChatCommand
{
    public string Command { get; } = "./serverpanel";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM || !caller.IsPlayerDM)
        {
            NWScript.SendMessageToAllDMs(
                $"{caller.PlayerName} tried to access the server control panel and is not a DM.");
            caller.SendServerMessage(
                message: "You must be a DM to use this command. This incident has been logged for posterity's sake.");
            return Task.CompletedTask;
        }

        caller.SendServerMessage(message: "This command is not yet supported.",
            Color.FromRGBA(rgbaHexString: "#8b0000"));
        return Task.CompletedTask;
    }
}