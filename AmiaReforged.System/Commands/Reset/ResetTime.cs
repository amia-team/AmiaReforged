using AmiaReforged.System.Services;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ResetTime : IChatCommand
{
    public string Command => "./uptime";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        caller.SendServerMessage($"Uptime: {(int) ResetTimeKeeperSingleton.Instance.Uptime() / 3600} hours.");
        return Task.CompletedTask;
    }
}