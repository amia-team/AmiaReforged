using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class ResetTime : IChatCommand
{
    public string Command => "./uptime";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        caller.SendServerMessage($"Uptime: {(int)ResetTimeKeeperSingleton.Instance.Uptime() / 3600} hours.");
        return Task.CompletedTask;
    }
}
