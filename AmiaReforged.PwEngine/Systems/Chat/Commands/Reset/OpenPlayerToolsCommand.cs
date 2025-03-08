using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class OpenPlayerToolsCommand : IChatCommand
{
    [Inject] private WindowManager WindowManager { get; set; }

    public string Command { get; } = "./playertools";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (caller.IsDM) return Task.CompletedTask;

        caller.SendServerMessage(
            message:
            "This command has been deprecated. Use the Player Tools feat (has a wrench icon) from your class's radial menu instead.",
            ColorConstants.Yellow);

        return Task.CompletedTask;
    }
}