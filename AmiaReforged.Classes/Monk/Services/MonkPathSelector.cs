using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Nui.MonkPath;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MonkPathSelector))]
public class MonkPathSelector
{
    private readonly WindowDirector _windowManager;

    public MonkPathSelector(WindowDirector windowManager)
    {
        _windowManager = windowManager;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += OpenPathSelectorWindow;
    }

    private void OpenPathSelectorWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeBase) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(MonkPathPresenter)))
        {
            player.FloatingTextString(message: "Path of Enlightenment selection is already open.", false);
            return;
        }

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (player.ControlledCreature != null && MonkUtils.GetMonkPath(player.ControlledCreature) != null && environment == "live")
        {
            player.FloatingTextString
            ("Path of Enlightenment has already been selected. " +
             "Rebuilding allows you to reselect your path.", false);

            return;
        }

        MonkPathView window = new(player);
        MonkPathPresenter presenter = window.Presenter;

        _windowManager.OpenWindow(presenter);
    }
}
