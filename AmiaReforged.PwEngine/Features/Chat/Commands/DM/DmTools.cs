using AmiaReforged.PwEngine.Features.DungeonMaster;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

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
