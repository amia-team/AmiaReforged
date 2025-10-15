using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Services;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class EconomyCommand(ResourceNodeInstanceSetupService nodeSetup) : IChatCommand
{
    public string Command => "./economy";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller is { IsDM: false, IsPlayerDM: false })
        {
            caller.SendServerMessage("This command is only available to DMs.");
            return Task.CompletedTask;
        }

        if (args.Length <= 0)
        {
            caller.SendServerMessage("Usage: ./economy init|destroy");
            return Task.CompletedTask;
        }

        switch (args[0])
        {
            case "init":
                HandleInit();
                break;
            case "destroy":
                HandleDestroy();
                break;
            default:
                caller.SendServerMessage("Usage: ./economy init|destroy");
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleDestroy()
    {
        nodeSetup.ClearOldNodes();
    }

    private void HandleInit()
    {
        nodeSetup.DoSetup();
    }
}
