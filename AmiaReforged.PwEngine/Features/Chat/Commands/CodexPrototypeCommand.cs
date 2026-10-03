using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Prototype;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

[ServiceBinding(typeof(IChatCommand))]
public sealed class CodexPrototypeCommand(WindowDirector windows) : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public string Command => "./codex-ui";
    public string Description => "Development-only graphical Codex prototype (toggle)";
    public string AllowedRoles => "All";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (UtilPlugin.GetEnvironmentVariable("SERVER_MODE") == "live")
        {
            caller.SendServerMessage("The Codex UI prototype is disabled on the live server.", ColorConstants.Orange);
            return Task.CompletedTask;
        }

        if (windows.IsWindowOpen(caller, typeof(CodexPrototypePresenter)))
        {
            windows.CloseWindow(caller, typeof(CodexPrototypePresenter));
            return Task.CompletedTask;
        }

        try
        {
            windows.OpenWindow(new CodexPrototypePresenter(caller));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open Codex UI prototype for {Player}", caller.PlayerName);
            caller.SendServerMessage("Could not open the Codex UI prototype. Check the server log.", ColorConstants.Orange);
        }
        return Task.CompletedTask;
    }
}
