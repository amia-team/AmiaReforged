using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(SpiritTechniqueHandler))]
public class SpiritTechniqueHandler
{
    private static readonly NwFeat? SpiritKiPointFeat = NwFeat.FromFeatId(MonkFeat.SpiritKiPoint);

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public SpiritTechniqueHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        // Register method to listen for the OnSpellCast event.
        NwModule.Instance.OnSpellCast += CastSpiritTechnique;
        Log.Info(message: "Monk Spirit Technique Handler initialized.");
    }

    private void CastSpiritTechnique(OnSpellCast castData)
    {
        if (castData.Caster is not NwCreature monk) return;
        if (castData.Spell?.FeatReference is null) return;
        if (monk.GetClassInfo(ClassType.Monk) is null) return;
        if (SpiritKiPointFeat == null) return;

        int technique = castData.Spell.FeatReference.Id;
        bool isSpiritTechnique = technique is MonkFeat.KiShout or MonkFeat.QuiveringPalmNew;

        if (!isSpiritTechnique) return;

        string abilityName = castData.Spell.FeatReference.Name.ToString();

        if (AbilityRestricted(monk, abilityName))
        {
            castData.PreventSpellCast = true;
            return;
        }

        switch (technique)
        {
            case MonkFeat.KiShout:
                KiShout.CastKiShout(castData);
                break;
            case MonkFeat.QuiveringPalmNew:
                QuiveringPalm.CastQuiveringPalm(castData);
                break;
        }

        monk.DecrementRemainingFeatUses(SpiritKiPointFeat);
    }

    private static bool AbilityRestricted(NwCreature monk, string abilityName)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand) is not null
                                      && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is
                                          BaseItemCategory.Torches;
        bool noSpiritKi = SpiritKiPointFeat != null && (!monk.KnowsFeat(SpiritKiPointFeat) || monk.GetFeatRemainingUses(SpiritKiPointFeat) < 1);

        if (monk.IsPlayerControlled(out NwPlayer? player))
        {
            if (hasArmor)
                player.SendServerMessage($"Cannot use {abilityName} because you are wearing armor.");
            if (hasShield)
                player.SendServerMessage($"Cannot use {abilityName} because you are wielding a shield.");
            if (hasFocusWithoutUnarmed)
                player.SendServerMessage($"Cannot use {abilityName} because you are wielding a focus without being unarmed.");
            if (noSpiritKi)
                player.SendServerMessage($"Cannot use {abilityName} because you have no Spirit Ki Points left.");
        }

        return hasArmor || hasShield || hasFocusWithoutUnarmed || noSpiritKi;
    }
}
