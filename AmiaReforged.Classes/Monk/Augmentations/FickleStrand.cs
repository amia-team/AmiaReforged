using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Spells;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public class FickleStrand : IAugmentation
{
    public PathType PathType => PathType.FickleStrand;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        switch (technique)
        {
            case TechniqueType.EagleStrike:
                AugmentEagleStrike(monk, attackData);
                break;
            case TechniqueType.AxiomaticStrike:
                AugmentAxiomaticStrike(monk, attackData);
                break;
            case TechniqueType.StunningStrike:
                StunningStrike.DoStunningStrike(attackData);
                break;
        }
    }

    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        switch (technique)
        {
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(monk);
                break;
            case TechniqueType.WholenessOfBody:
                WholenessOfBody.DoWholenessOfBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    /// <summary>
    /// Eagle Strike has a 30% chance to impart a wild magic effect.
    /// Each Ki Focus makes potent effects more likely to occur.
    /// </summary>
    private void AugmentEagleStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        EagleStrike.DoEagleStrike(monk, attackData);

        if (attackData.Target is not NwCreature targetCreature || !targetCreature.IsReactionTypeHostile(monk)) return;

        int d100Roll = Random.Shared.Roll(100);

        if (d100Roll <= 30)
            WildMagicEffects.DoWildMagic(monk, targetCreature);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus magical damage. Each Ki Focus increases the damage by 1,
    /// to a maximum of +4 bonus magical damage.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        short bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        DamageData<short> damageData = attackData.DamageData;
        short magicalDamage = damageData.GetDamageByType(DamageType.Magical);

        magicalDamage += bonusDamage;
        damageData.SetDamageByType(DamageType.Magical, magicalDamage);
    }

    /// <summary>
    /// Empty Body grants a spell mantle that absorbs up to 2 spells and spell-like abilities.
    /// Each Ki Focus increases the effects it can absorb by 2, to a maximum of 8 spells or spell-like abilities.
    /// </summary>
    private  void AugmentEmptyBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        int totalSpellsAbsorbed = Random.Shared.Roll(3, diceAmount);

        Effect spellAbsorb = Effect.LinkEffects(
            Effect.SpellLevelAbsorption(9,totalSpellsAbsorbed),
            Effect.VisualEffect(VfxType.DurSpellturning)
        );
        spellAbsorb.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Temporary, spellAbsorb, NwTimeSpan.FromRounds(monkLevel));
    }

    /// <summary>
    /// Ki Shout deals magical damage instead of sonic. In addition, it breaches enemy creatures of 1 magical defense
    /// according to the breach list. Each Ki Focus adds an additional breached magical defense, to a maximum of 4 magical effects.
    /// </summary>
    private  void AugmentKiShout(NwCreature monk)
    {
        KiShout.DoKiShout(monk, DamageType.Magical, VfxType.ImpMagblue);

        if (monk.Location == null) return;

        int spellsBreached = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (nwObject is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            DoBreach(hostileCreature, spellsBreached);
        }
    }

    private static void DoBreach(NwCreature targetCreature, int breachAmount)
    {
        int breachedCount = 0;

        foreach (Spell spell in BreachList.BreachSpells)
        {
            Effect? effectToBreach = targetCreature.ActiveEffects.FirstOrDefault(effect => effect.Spell?.SpellType == spell);

            if (effectToBreach == null) continue;

            targetCreature.RemoveEffect(effectToBreach);
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpBreach));
            breachedCount++;

            if (breachedCount >= breachAmount) break;
        }
    }
}
