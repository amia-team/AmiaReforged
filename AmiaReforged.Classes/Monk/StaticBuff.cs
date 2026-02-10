using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk;

public static class StaticBuff
{
    private static readonly NwFeat? MonkDefenseFeat = NwFeat.FromFeatId(MonkFeat.MonkDefense);
    private static readonly NwFeat? MonkSpeedFeat = NwFeat.FromFeatId(MonkFeat.MonkSpeedNew);
    private const string StaticBuffTag = "monk_static_buff";

    public static void RefreshBuff(NwCreature monk)
    {
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_EQUIP_SHIELD_AC_NO_STACK, 1, monk);

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect? existingMonkBuff = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == StaticBuffTag);

        if (existingMonkBuff != null)
        {
            monk.RemoveEffect(existingMonkBuff);
            SetMovementSpeedCap(monk, 0);
        }

        if (MonkUtils.AbilityRestricted(monk, "monk abilities"))
        {
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_EQUIP_SHIELD_AC_NO_STACK, 0, monk);
            return;
        }

        List<Effect> effectsToLink = [];

        if (!monk.KnowsFeat(Feat.MonkAcBonus!) && MonkDefenseFeat != null && monk.KnowsFeat(MonkDefenseFeat))
        {
            int wisMod = monk.GetAbilityModifier(Ability.Wisdom);
            Effect monkDefenseEffect = MonkDefense(monkLevel, wisMod);
            monkDefenseEffect.ShowIcon = false;
            effectsToLink.Add(monkDefenseEffect);
        }

        if (!monk.KnowsFeat(Feat.MonkEndurance!) && MonkSpeedFeat != null && monk.KnowsFeat(MonkSpeedFeat))
        {
            Effect monkSpeedEffect = MonkSpeed(monkLevel);
            monkSpeedEffect.ShowIcon = false;
            effectsToLink.Add(monkSpeedEffect);

            SetMovementSpeedCap(monk, monkLevel);
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

        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_EQUIP_SHIELD_AC_NO_STACK, 0, monk);
    }

    /// <summary>
    /// Raises the monk speed cap according to monk level. If monk level is passed as 0, reverts monk speed cap to default.
    /// </summary>
    private static void SetMovementSpeedCap(NwCreature monk, int monkLevel)
    {
        float movementSpeedCap = monkLevel switch
        {
            >= 4 and <= 9 => 1.6f,
            >= 10 and <= 15 => 1.7f,
            >= 16 and <= 21 => 1.8f,
            >= 22 and <= 27 => 1.9f,
            >= 28 => 2.0f,
            _ => 1.5f
        };
        CreaturePlugin.SetMovementRateFactorCap(monk, movementSpeedCap);
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
            >= 4 and <= 9 => 10,
            >= 10 and <= 15 => 20,
            >= 16 and <= 21 => 30,
            >= 22 and <= 27 => 40,
            >= 28 => 50,
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
}
