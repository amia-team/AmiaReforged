using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Ranger;

[ServiceBinding(typeof(EpicCompanionFeatHandler))]
public class EpicCompanionFeatHandler
{
    private const string EpicCompanionAppearanceVar = "epic_companion_appearance";
    private const string PcKeyTag = "ds_pckey";
    private const int EpicCompanionFeatId = 1240;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EpicCompanionFeatHandler()
    {
        NwModule.Instance.OnUseFeat += ToggleEpicCompanionAppearance;

        Log.Info("Epic Companion Feat Handler initialized.");
    }

    private void ToggleEpicCompanionAppearance(OnUseFeat eventData)
    {
        if (eventData.Feat.Id != EpicCompanionFeatId) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwItem? pcKey = eventData.Creature.Inventory.Items.FirstOrDefault(item => item.Tag == PcKeyTag);
        if (pcKey == null) return;

        LocalVariableInt epicCompanionAppearance = pcKey.GetObjectVariable<LocalVariableInt>(EpicCompanionAppearanceVar);

        if (epicCompanionAppearance.Value == 0)
        {
            epicCompanionAppearance.Value = 1;
            player.SendServerMessage("Opted in for the Epic Companion appearance.");
            return;
        }

        epicCompanionAppearance.Delete();
        player.SendServerMessage("Opted out of the Epic Companion appearance.");
    }
}
