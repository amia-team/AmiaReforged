using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class StaticBuff
{
    private static readonly NwFeat? MonkDefenseFeat = NwFeat.FromFeatId(MonkFeat.MonkDefense);
    private static readonly NwFeat? MonkSpeedFeat = NwFeat.FromFeatId(MonkFeat.MonkSpeedNew);
    private const string StaticBuffTag = "monk_static_buff";

    public static void AdjustBuff(NwCreature monk)
    {
        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect? existingMonkBuff = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == StaticBuffTag);

        if (existingMonkBuff != null)
                monk.RemoveEffect(existingMonkBuff);

        bool abilitiesRestricted = AbilityRestricted(monk);

        if (MonkUtils.GetMonkPath(monk) == PathType.HiddenSpring)
        {
            WisdomAttackBonus.AdjustWisdomAttackBonus(monk, abilitiesRestricted);
        }

        if (abilitiesRestricted) return;

        List<Effect> effectsToLink = [];

        if (!monk.KnowsFeat(Feat.MonkAcBonus!) && MonkDefenseFeat != null && monk.KnowsFeat(MonkDefenseFeat))
        {
            int wisMod = monk.GetAbilityModifier(Ability.Wisdom);
            Effect monkDefenseEffect = MonkDefense(monkLevel, wisMod);
            monkDefenseEffect.ShowIcon = false;
            effectsToLink.Add(monkDefenseEffect);
        }

        if (MonkSpeedFeat != null && monk.KnowsFeat(MonkSpeedFeat))
        {
            Effect monkSpeedEffect = MonkSpeed(monkLevel);
            monkSpeedEffect.ShowIcon = false;
            effectsToLink.Add(monkSpeedEffect);
        }

        KiFocus? kiFocusTier = MonkUtils.GetKiFocus(monk);
        if (kiFocusTier != null)
        {
            Effect kiFocusAbEffect = KiFocusAb(kiFocusTier);
            kiFocusAbEffect.ShowIcon = false;
            effectsToLink.Add(kiFocusAbEffect);
        }

        Effect? monkBuff =
            effectsToLink.Count switch
            {
                1 => effectsToLink[0],
                2 => Effect.LinkEffects(effectsToLink[0], effectsToLink[1]),
                3 => Effect.LinkEffects(effectsToLink[0], effectsToLink[1], effectsToLink[2]),
                _ => null
            };

        if (monkBuff == null) return;

        monkBuff.ShowIcon = false;
        monkBuff.SubType = EffectSubType.Unyielding;
        monkBuff.Tag = StaticBuffTag;

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
    =>  kiFocusTier switch
        {
            KiFocus.KiFocus1 => Effect.AttackIncrease(1),
            KiFocus.KiFocus2 => Effect.AttackIncrease(2),
            KiFocus.KiFocus3 => Effect.AttackIncrease(3),
            _ => Effect.AttackIncrease(0)
        };

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
