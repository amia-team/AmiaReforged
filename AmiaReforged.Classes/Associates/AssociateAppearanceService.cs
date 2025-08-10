using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Associates;

[ServiceBinding(typeof(AssociateAppearanceService))]
public class AssociateAppearanceService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string AssociateCustomizerTag = "ass_customizer";
    private const string PcKeyTag = "ds_pckey";


    public AssociateAppearanceService()
    {
        NwModule.Instance.OnAssociateAdd += ChangeAssociateAppearance;
        Log.Info("Associate Bonus Service initialized.");
    }

    private void ChangeAssociateAppearance(OnAssociateAdd eventData)
    {
        NwItem? pcKey = eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == PcKeyTag);

        NwItem? associateCustomizer = eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == AssociateCustomizerTag);

        bool associateHasCustomAppearance =
            CustomAppearance.ApplyCustomAppearance(eventData.Associate, associateCustomizer);

        if (associateHasCustomAppearance) return;

        bool associateHasEpicCompanionAppearance =
            EpicCompanionAppearance.ApplyEpicCompanionAppearance(eventData.Associate, eventData.Owner, pcKey);

        if (associateHasEpicCompanionAppearance) return;


    }


}
