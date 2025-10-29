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
                HandleInit(caller);
                break;
            case "destroy":
                HandleDestroy(caller);
                break;
            default:
                caller.SendServerMessage("Usage: ./economy init|destroy");
                break;
        }

        return Task.CompletedTask;
    }

    private void HandleDestroy(NwPlayer caller)
    {
        caller.SendServerMessage("Clearing all resource nodes...");
        nodeSetup.ClearOldNodes();
        caller.SendServerMessage("✓ Resource nodes cleared");
    }

    private void HandleInit(NwPlayer caller)
    {
        caller.SendServerMessage("Initializing resource node economy...");
        nodeSetup.DoSetup();
        caller.SendServerMessage("✓ Economy initialization complete");
    }
}
