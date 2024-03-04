using AmiaReforged.Core.UserInterface;
using AmiaReforged.System.UI.PlayerTools;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.System.Commands.Reset;

[ServiceBinding(typeof(IChatCommand))]
public class OpenPlayerToolsCommand : IChatCommand
{
    [Inject] private WindowManager WindowManager { get; set; }
    
    public string Command { get; } = "./playertools";
    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if(caller.IsDM) return Task.CompletedTask;
        
        caller.SendServerMessage("Opening Player Tools.");
        
        WindowManager.OpenWindow<PlayerToolButtonView>(caller);
        return Task.CompletedTask;
    }
}