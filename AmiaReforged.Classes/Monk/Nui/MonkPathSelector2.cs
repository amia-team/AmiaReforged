using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Nui.MonkPath;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Nui;

[ServiceBinding(typeof(MonkPathSelector2))]
public class MonkPathSelector2
{
    private readonly WindowDirector _windowManager;

    public MonkPathSelector2(WindowDirector windowManager)
    {
        _windowManager = windowManager;
        NwModule.Instance.OnUseFeat += OpenPathSelectorWindow;
    }

    private void OpenPathSelectorWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeBase) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(MonkPathPresenter)))
        {
            player.FloatingTextString(message: "Player Tools window is already open.", false);
            return;
        }

        MonkPathView window = new(player);
        MonkPathPresenter presenter = window.Presenter;

        _windowManager.OpenWindow(presenter);
    }
}
