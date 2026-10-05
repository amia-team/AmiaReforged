using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Nui.Prototype;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

[ServiceBinding(typeof(IChatCommand))]
public sealed class DialoguePrototypeCommand(WindowDirector windows, DevicePropertyService device) : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public string Command => "./dialogueui";
    public string Description => "Development-only dialogue artwork preview: standard, long, empty, single, overflow, last, speaker, 0..5 choices; optional corner";
    public string AllowedRoles => "All";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (UtilPlugin.GetEnvironmentVariable("SERVER_MODE") == "live")
        {
            caller.SendServerMessage("The dialogue UI preview is disabled on the live server.", ColorConstants.Orange);
            return Task.CompletedTask;
        }

        string scenario = args.Length == 0 ? "standard" : args[0].ToLowerInvariant();
        int? choices = null;
        if (int.TryParse(scenario, out int count) && count is >= 0 and <= 5)
        {
            scenario = "choices";
            choices = count;
        }
        bool validScenario = choices.HasValue || scenario is "standard" or "long" or "empty" or "single" or "overflow" or "last" or "speaker";
        if (!validScenario ||
            args.Length > 2 || (args.Length == 2 && !string.Equals(args[1], "corner", StringComparison.OrdinalIgnoreCase)))
        {
            caller.SendServerMessage("Usage: ./dialogueui [standard|long|empty|single|overflow|last|speaker|0..5] [corner]. Repeat ./dialogueui to close.", ColorConstants.Orange);
            return Task.CompletedTask;
        }

        bool open = windows.IsWindowOpen(caller, typeof(ConversationPrototypePresenter));
        if (open) windows.CloseWindow(caller, typeof(ConversationPrototypePresenter));
        if (open && args.Length == 0) return Task.CompletedTask;
        try
        {
            windows.OpenWindow(new ConversationPrototypePresenter(caller, device, scenario, args.Length == 2, choices));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open dialogue UI preview for {Player}", caller.PlayerName);
            caller.SendServerMessage("Could not open the dialogue UI preview. Check the server log.", ColorConstants.Orange);
        }
        return Task.CompletedTask;
    }
}
