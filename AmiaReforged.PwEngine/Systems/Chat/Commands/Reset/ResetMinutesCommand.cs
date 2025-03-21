﻿using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ResetMinutesCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string Command => "./resetminutes";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM)
        {
            NWScript.SendMessageToAllDMs(
                $"{caller.PlayerName} tried changing the reset timer of the server and is not a DM.");
            caller.SendServerMessage(
                message: "You must be a DM to use this command. This incident has been logged for posterity's sake.");
            Log.Warn($"{caller.PlayerName} tried changing the reset time of the server and is not a DM.");
            return Task.CompletedTask;
        }

        if (args.Length == 0)
        {
            caller.SendServerMessage(
                message: "./resetminutes usage: \"./resetminutes <number>\" for example, \"./resetminutes 30\"");
            return Task.CompletedTask;
        }

        try
        {
            float newReset = float.Parse(args[0]);
            NWScript.SetLocalFloat(NwModule.Instance, sVarName: "minutesToReset", newReset);
            ResetTimeKeeperSingleton.Instance.ResetStartTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();

            NwModule.Instance.SendMessageToAllDMs($"Amia reset timer has been changed to {newReset} minutes");
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            caller.SendServerMessage(
                message: "Invalid input. Please use a number and strict spacing. For example: ./resetminutes 30.");
        }

        return Task.CompletedTask;
    }
}