using AmiaReforged.PwEngine.Systems.DungeonMaster;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class DmTools(WindowDirector director) : IChatCommand
{
    public string Command => "./dmtools";
    public Task ExecuteCommand(NwPlayer caller, string[] args)
    { 
        if (!caller.IsDM) return Task.CompletedTask;

        DmToolView view = new(caller);
        
        director.OpenWindow(view.Presenter);
        
        return Task.CompletedTask;
    }
}