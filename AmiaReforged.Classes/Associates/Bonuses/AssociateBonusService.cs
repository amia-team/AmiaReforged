using AmiaReforged.Classes.Shadowdancer.Shadow;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Associates.Bonuses;

[ServiceBinding(typeof(AssociateBonusService))]
public class AssociateBonusService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public AssociateBonusService()
    {
        NwModule.Instance.OnAssociateAdd += ApplyAssociateBonus;
        Log.Info("Associate Bonus Service initialized.");
    }

    private void ApplyAssociateBonus(OnAssociateAdd eventData)
    {
        switch (eventData.AssociateType)
        {
            case AssociateType.AnimalCompanion:
                CompanionBonuses companionBonuses = new(eventData.Owner, eventData.Associate);
                companionBonuses.ApplyCompanionBonus();
                break;
            case AssociateType.Familiar:
                FamiliarBonuses familiarBonuses = new(eventData.Owner, eventData.Associate);
                familiarBonuses.ApplyFamiliarBonus();
                break;
            case AssociateType.Summoned:
                if (eventData.Associate.ResRef.StartsWith("sd_shadow_"))
                    ShadowBonuses.ApplyShadowBonuses(eventData.Owner, eventData.Associate);
                break;
        }
    }
}
