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
    private const string ReskinWidgetTag = "ds_summ_change";


    public AssociateAppearanceService()
    {
        NwModule.Instance.OnAssociateAdd += ChangeAssociateAppearance;
        Log.Info("Associate Bonus Service initialized.");
    }

    private void ChangeAssociateAppearance(OnAssociateAdd eventData)
    {
        NwItem? associateCustomizer = eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == AssociateCustomizerTag);

        bool associateHasCustomAppearance =
            CustomAppearance.ApplyCustomAppearance(eventData.Associate, eventData.AssociateType, associateCustomizer);

        if (associateHasCustomAppearance) return;

        NwItem? legacyReskinWidget =  eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == ReskinWidgetTag);

        bool associateHasLegacyReskin =
            LegacySummonReskin.ApplySummonReskin(eventData.Associate, eventData.AssociateType, legacyReskinWidget);

        if (associateHasLegacyReskin) return;

        NwItem? pcKey = eventData.Owner.Inventory.Items.FirstOrDefault(item => item.Tag == PcKeyTag);

        bool associateHasEpicCompanionAppearance =
            EpicCompanionAppearance.ApplyEpicCompanionAppearance(eventData.Associate, eventData.Owner, pcKey);

        if (associateHasEpicCompanionAppearance) return;
    }
}
