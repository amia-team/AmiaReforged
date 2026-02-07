using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

[ServiceBinding(typeof(MonkPathSelector))]
public class MonkPathSelector
{
    private readonly WindowDirector _windowManager;

    public MonkPathSelector(WindowDirector windowManager)
    {
        _windowManager = windowManager;
        NwModule.Instance.OnUseFeat += OpenPathSelectionWindow;
    }

    private void OpenPathSelectionWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PoeBase) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(MonkPathPresenter)))
        {
            player.FloatingTextString(message: "Path of Enlightenment selection is already open.", false);
            return;
        }

        MonkPathView window = new(player);
        MonkPathPresenter presenter = window.Presenter;

        _windowManager.OpenWindow(presenter);
    }
}
