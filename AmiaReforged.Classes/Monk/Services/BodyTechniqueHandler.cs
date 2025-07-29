using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(BodyTechniqueHandler))]
public class BodyTechniqueHandler
{
    private static readonly NwFeat? BodyKiPointFeat = NwFeat.FromFeatId(MonkFeat.BodyKiPoint);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public BodyTechniqueHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        // Register method to listen for the OnSpellCast event.
        NwModule.Instance.OnSpellCast += CastBodyTechnique;
        Log.Info(message: "Monk Body Technique Handler initialized.");
    }

    private static void CastBodyTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (castData.Spell?.FeatReference is null) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;
        if (BodyKiPointFeat == null) return;

        int technique = castData.Spell.FeatReference.Id;
        bool isBodyTechnique = technique is MonkFeat.EmptyBodyNew or MonkFeat.KiBarrier or MonkFeat.WholenessOfBodyNew;
        if (!isBodyTechnique) return;

        string abilityName = castData.Spell.FeatReference.Name.ToString();

        if (AbilityRestricted(monk, abilityName))
        {
            castData.PreventSpellCast = true;
            return;
        }

        switch (technique)
        {
            case MonkFeat.WholenessOfBodyNew:
                WholenessOfBody.CastWholenessOfBody(castData);
                break;
            case MonkFeat.EmptyBodyNew:
                EmptyBody.CastEmptyBody(castData);
                break;
            case MonkFeat.KiBarrier:
                KiBarrier.CastKiBarrier(castData);
                break;
        }

        monk.DecrementRemainingFeatUses(BodyKiPointFeat);
    }

    private static bool AbilityRestricted(NwCreature monk, string abilityName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand) is not null
                                      && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is
                                          BaseItemCategory.Torches;
        bool noBodyKi = BodyKiPointFeat != null && (!monk.KnowsFeat(BodyKiPointFeat) || monk.GetFeatRemainingUses(BodyKiPointFeat) < 1);

        if (monk.IsPlayerControlled(out NwPlayer? player))
        {
            if (hasArmor)
                player.SendServerMessage($"Cannot use {abilityName} because you are wearing armor.");
            if (hasShield)
                player.SendServerMessage($"Cannot use {abilityName} because you are wielding a shield.");
            if (hasFocusWithoutUnarmed)
                player.SendServerMessage($"Cannot use {abilityName} because you are wielding a focus without being unarmed.");
            if (noBodyKi)
                player.SendServerMessage($"Cannot use {abilityName} because you have no Body Ki Points left.");
        }

        return hasArmor || hasShield || hasFocusWithoutUnarmed || noBodyKi;
    }
}
