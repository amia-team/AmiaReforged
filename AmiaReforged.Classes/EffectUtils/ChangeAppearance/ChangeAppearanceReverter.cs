using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.EffectUtils.ChangeAppearance;

[ServiceBinding(typeof(ChangeAppearanceReverter))]
public class ChangeAppearanceReverter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ChangeAppearanceReverter()
    {
        NwModule.Instance.OnClientEnter += RevertChangedAppearance;

        Log.Info("Change Appearance Reverter initialized.");
    }

    private void RevertChangedAppearance(ModuleEvents.OnClientEnter eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature
            || ChangeAppearanceUtils.HasActiveChangeAppearance(creature))
            return;

        ChangeAppearanceData? originalAppearance = ChangeAppearanceUtils.GetOriginalAppearance(creature);
        if (originalAppearance == null) return;

        ChangeAppearanceUtils.SetAppearance(creature, originalAppearance);
        ChangeAppearanceUtils.ClearOriginalAppearance(creature);

        Log.Info("Reverted appearance for {0}.", creature.Name);
    }
}
