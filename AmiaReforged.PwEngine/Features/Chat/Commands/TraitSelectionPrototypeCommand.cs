using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Prototype;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

[ServiceBinding(typeof(IChatCommand))]
public sealed class TraitSelectionPrototypeCommand(WindowDirector windows, DevicePropertyService device) : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public string Command => "./traitsui";
    public string Description => "Development-only trait artwork preview: standard, full, overflow, last, long, remove, confirmed, disabled, debt, zero, category, compact, collapse, error, 0..32; optional corner";
    public string AllowedRoles => "All";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (UtilPlugin.GetEnvironmentVariable("SERVER_MODE") == "live")
        {
            caller.SendServerMessage("The trait UI preview is disabled on the live server.", ColorConstants.Orange);
            return Task.CompletedTask;
        }
        string scenario = args.Length == 0 ? "standard" : args[0].ToLowerInvariant();
        int? count = null;
        if (int.TryParse(scenario, out int number) && number is >= 0 and <= 32)
        {
            scenario = "rows";
            count = number;
        }
        bool valid = count.HasValue || scenario is "standard" or "empty" or "single" or "full" or "overflow" or "last"
            or "long" or "remove" or "confirmed" or "disabled" or "debt" or "zero" or "category" or "compact" or "collapse" or "error";
        if (!valid || args.Length > 2 || (args.Length == 2 && !string.Equals(args[1], "corner", StringComparison.OrdinalIgnoreCase)))
        {
            caller.SendServerMessage("Usage: ./traitsui [standard|empty|single|full|overflow|last|long|remove|confirmed|disabled|debt|zero|category|compact|collapse|error|0..32] [corner]. Repeat ./traitsui to close.", ColorConstants.Orange);
            return Task.CompletedTask;
        }
        bool open = windows.IsWindowOpen(caller, typeof(TraitSelectionPrototypePresenter));
        if (open) windows.CloseWindow(caller, typeof(TraitSelectionPrototypePresenter));
        if (open && args.Length == 0) return Task.CompletedTask;
        try
        {
            windows.OpenWindow(new TraitSelectionPrototypePresenter(caller, device, scenario, args.Length == 2, count));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open trait UI preview for {Player}", caller.PlayerName);
            caller.SendServerMessage("Could not open the trait UI preview. Check the server log.", ColorConstants.Orange);
        }
        return Task.CompletedTask;
    }
}
