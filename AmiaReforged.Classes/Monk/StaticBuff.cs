using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public class StaticBuff
{
    private static readonly NwFeat? MonkDefenseFeat = NwFeat.FromFeatId(MonkFeat.MonkDefense);
    private static readonly NwFeat? MonkSpeedFeat = NwFeat.FromFeatId(MonkFeat.MonkSpeedNew);

    public static void AdjustBuff(NwCreature monk)
    {
        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect? monkBuff = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_static_buff");

        if (monkBuff != null)
                monk.RemoveEffect(monkBuff);

        bool abilitiesRestricted = AbilityRestricted(monk);

        if (MonkUtils.GetMonkPath(monk) == PathType.HiddenSpring)
        {
            WisdomAttackBonus.AdjustWisdomAttackBonus(monk, abilitiesRestricted);
        }

        if (abilitiesRestricted) return;

        if (MonkDefenseFeat != null && monk.KnowsFeat(MonkDefenseFeat))
        {
            int wisMod = monk.GetAbilityModifier(Ability.Wisdom);

            Effect monkDefenseEffect = MonkDefense(monkLevel, wisMod);
            monkBuff = Effect.LinkEffects(monkDefenseEffect);
        }

        if (MonkSpeedFeat != null && monk.KnowsFeat(MonkSpeedFeat))
        {
            Effect monkSpeedEffect = MonkSpeed(monkLevel);
            monkBuff = Effect.LinkEffects(monkSpeedEffect);
        }

        KiFocus? kiFocusTier = MonkUtils.GetKiFocus(monk);

        if (kiFocusTier != null)
        {
            Effect kiFocusAb = KiFocusAb(kiFocusTier);
            monkBuff = Effect.LinkEffects(kiFocusAb);
        }

        if (monkBuff == null) return;

        monkBuff.ShowIcon = false;
        monkBuff.SubType = EffectSubType.Unyielding;
        monkBuff.Tag = "monk_static_buff";

        monk.ApplyEffect(EffectDuration.Permanent, monkBuff);
    }

    private static Effect MonkDefense(int monkLevel, int wisMod)
    {
        int acBonusAmount = monkLevel >= wisMod ? wisMod : monkLevel;
        return Effect.ACIncrease(acBonusAmount, ACBonus.ShieldEnchantment);
    }

    private static Effect MonkSpeed(int monkLevel)
    {
        int monkSpeedBonusAmount = monkLevel switch
        {
            >= 4 and <= 10 => 10,
            >= 11 and <= 16 => 20,
            >= 17 and <= 21 => 30,
            >= 22 and <= 26 => 40,
            >= 27 => 50,
            _ => 0
        };

        return Effect.MovementSpeedIncrease(monkSpeedBonusAmount);
    }

    private static Effect KiFocusAb(KiFocus? kiFocusTier)
    {
        int abAmount = kiFocusTier switch
        {
            KiFocus.KiFocus1 => 1,
            KiFocus.KiFocus2 => 2,
            KiFocus.KiFocus3 => 3,
            _ => 0
        };

        return Effect.AttackIncrease(abAmount);
    }

    private static bool AbilityRestricted(NwCreature monk)
    {
        bool hasArmor = monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;
        bool hasShield = monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is BaseItemCategory.Shield;
        bool hasFocusWithoutUnarmed = monk.GetItemInSlot(InventorySlot.RightHand) is not null
                                      && monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category is
                                          BaseItemCategory.Torches;

        if (monk.IsPlayerControlled(out NwPlayer? player))
        {
            if (hasArmor)
                player.SendServerMessage("Equipping this armor has disabled your monk abilities.");
            if (hasShield)
                player.SendServerMessage("Equipping this shield has disabled your monk abilities.");
            if (hasFocusWithoutUnarmed)
                player.SendServerMessage("Equipping a focus without being unarmed has disabled your monk abilities.");
        }

        return hasArmor || hasShield || hasFocusWithoutUnarmed;
    }
}
