using AmiaReforged.Classes.Warlock.Constants;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.Nui.WarlockPact;

[ServiceBinding(typeof(WarlockPactSelector))]
public class WarlockPactSelector
{
    private readonly WindowDirector _windowManager;

    public WarlockPactSelector(WindowDirector windowManager)
    {
        _windowManager = windowManager;
        NwModule.Instance.OnUseFeat += OpenPathSelectionWindow;
    }

    private void OpenPathSelectionWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.FeatType is not WarlockFeat.PactBase) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(WarlockPactPresenter)))
        {
            player.FloatingTextString(message: "Pact selection is already open.", false);
            return;
        }

        WarlockPactView window = new(player);
        WarlockPactPresenter presenter = window.Presenter;

        _windowManager.OpenWindow(presenter);
    }
}
