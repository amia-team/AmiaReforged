using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Nui.EyeGlow;

[ServiceBinding(typeof(EyeGlowSelector))]
public class EyeGlowSelector
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;

    public EyeGlowSelector(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += OpenEyeGlowSelectionWindow;

        _log.Info("Monk Eye Glow Selector initialized.");
    }

    private void OpenEyeGlowSelectionWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.FeatType is not Feat.PerfectSelf) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (eventData.Creature.ActiveEffects.Any(e => e.Tag == EyeGlowModel.PermanentGlowTag))
        {
            player.FloatingTextString(message: "You have already selected your eye glow. To reselect your eye glow, " +
                                               "ask a DM to remove your current one.", false);
            return;
        }

        if (_windowDirector.IsWindowOpen(player, typeof(EyeGlowPresenter)))
        {
            player.FloatingTextString(message: "Eye Glow selection is already open.", false);
            return;
        }

        EyeGlowView window = new(player);
        EyeGlowPresenter presenter = window.Presenter;

        _windowDirector.OpenWindow(presenter);
    }
}
