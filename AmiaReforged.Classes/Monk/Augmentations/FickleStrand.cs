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
            case TechniqueType.QuiveringPalm:
                AugmentQuiveringPalm(monk, castData);
                break;
            case TechniqueType.WholenessOfBody:
                WholenessOfBody.DoWholenessOfBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(monk);
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

        if (attackData.Target is not NwCreature targetCreature || !monk.IsReactionTypeHostile(targetCreature)) return;

        if (Random.Shared.Roll(100) <= 30)
            WildMagicEffects.DoWildMagic(monk, targetCreature);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus magical damage. Each Ki Focus increases the damage by 1,
    /// to a maximum of +4 bonus magical damage.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        DamageData<short> damageData = attackData.DamageData;
        short magicalDamage = damageData.GetDamageByType(DamageType.Magical);

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (magicalDamage == -1) bonusDamage++;

        magicalDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Magical, magicalDamage);
    }

    /// <summary>
    /// Empty Body grants a spell mantle that absorbs up to 2 spells and spell-like abilities.
    /// Each Ki Focus increases the effects it can absorb by 2, to a maximum of 8 spells or spell-like abilities.
    /// </summary>
    private  void AugmentEmptyBody(NwCreature monk)
    {
        EmptyBody.DoEmptyBody(monk);
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
            Effect.SpellLevelAbsorption(9, totalSpellsAbsorbed),
            Effect.VisualEffect(VfxType.DurSpellturning)
        );
        spellAbsorb.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Temporary, spellAbsorb, NwTimeSpan.FromRounds(monkLevel));
    }

    /// <summary>
    /// Quivering Palm strips the enemy creature of a magical defense according to the breach list, with a 50% chance to
    /// steal the magical defense. Each Ki Focus adds an additional magical defense.
    /// </summary>
    private  void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (castData.TargetObject is not NwCreature targetCreature || touchAttackResult is TouchAttackResult.Miss)
            return;

        int spellsToSteal = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        var stealableSpellGroups = targetCreature.ActiveEffects
            .Where(e => e.Spell != null && BreachList.BreachSpells.Contains(e.Spell.SpellType))
            .GroupBy(e => e.Spell)
            .Select(spellGroup => new
            {
                Spell = spellGroup.Key,
                Effects = spellGroup.ToList(),
                Duration = spellGroup.First().DurationRemaining
            })
            .Take(spellsToSteal)
            .ToArray();


        if (stealableSpellGroups.Length == 0)
        {
            monk.ControllingPlayer?.FloatingTextString("No magical defenses to steal!"
                .ColorString(ColorConstants.Purple));

            return;
        }

        List<string?> stolenSpellNames = [];
        foreach (var spellGroup in stealableSpellGroups)
        {
            if (Random.Shared.Roll(2) == 1)
            {
                stolenSpellNames.Add(spellGroup.Spell?.Name.ToString());

                foreach (Effect effect in spellGroup.Effects)
                {
                    monk.ApplyEffect(EffectDuration.Temporary, effect, TimeSpan.FromSeconds(spellGroup.Duration));
                }
            }

            foreach (Effect effect in spellGroup.Effects)
            {
                targetCreature.RemoveEffect(effect);
            }
        }

        if (stolenSpellNames.Count > 0)
        {
            string stolenMessageList = string.Join(", ", stolenSpellNames);
            string finalMessage = $"You successfully steal {stolenMessageList.ColorString(ColorConstants.Purple)}";
            monk.ControllingPlayer?.FloatingTextString(finalMessage);
        }

        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpBreach));
    }
}
