using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.InflictMinorWounds;

[ServiceBinding(typeof(ISpell))]
public class InflictMinorWounds : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X0_S0_Inflict";
    

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        switch (eventData.Spell.SpellType)
        {
            case Spell.InflictMinorWounds:
                DoInflictMinorWounds(casterCreature, eventData);
                break;
            case Spell.InflictLightWounds:
                DoInflictLightWounds(casterCreature, eventData);
                break;
            default:
                return;
        }
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    #region Inflict Minor Wounds

    private void DoInflictMinorWounds(NwCreature casterCreature, SpellEvents.OnSpellCast eventData)
    {
        TouchAttackResult result = casterCreature.TouchAttackRanged(eventData.TargetObject!, true);

        bool skipTouchAttack = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD;
        
        SpellUtils.SignalSpell(casterCreature, eventData.TargetObject!, eventData.Spell);
        
        if (result != TouchAttackResult.Hit || !skipTouchAttack) return;

        int damage = CalculateMinorDamage(casterCreature);

        if (ResistedSpell || !skipTouchAttack) return;

        ApplyInflictEffect(eventData.TargetObject!, damage);
    }

    private static int CalculateMinorDamage(NwCreature casterCreature)
    {
        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int bonusDie = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;

        int numDie = casterCreature.CasterLevel / 2 + bonusDie;

        return NWScript.d3(numDie);
    }

    #endregion

    #region Inflict Light Wounds

    private void DoInflictLightWounds(NwCreature casterCreature, SpellEvents.OnSpellCast eventData)
    {
        NwGameObject target = eventData.TargetObject!;
        bool isUndead = NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD;

        // Touch attack — skip for undead (they get healed)
        if (!isUndead)
        {
            TouchAttackResult touchResult = casterCreature.TouchAttackMelee(target, true);
            if (touchResult == TouchAttackResult.Miss) return;
        }

        SpellUtils.SignalSpell(casterCreature, target, eventData.Spell);

        if (ResistedSpell) return;

        int casterLevel = Math.Min(casterCreature.CasterLevel, 15);

        // 1d8 + 1 per caster level (max +15)
        int baseDamage = SpellUtils.MaximizeSpell(eventData.MetaMagicFeat, 8, 1) + casterLevel;
        baseDamage = SpellUtils.EmpowerSpell(eventData.MetaMagicFeat, baseDamage);

        // Will save for half on non-undead
        if (!isUndead)
        {
            int dc = SpellUtils.GetSpellDc(eventData);
            SavingThrowResult saveResult =
                ((NwCreature)target).RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Negative, casterCreature);

            if (saveResult == SavingThrowResult.Success)
                baseDamage /= 2;
        }

        if (baseDamage <= 0) return;

        ApplyInflictEffect(target, baseDamage);

        // Lingering DoT / HoT
        int lingeringDice = Math.Max(1, casterCreature.CasterLevel / 3);

        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int lingeringRounds = 1;
        if (hasEpicFocus) lingeringRounds = 3;
        else if (hasGreaterFocus) lingeringRounds = 3;
        else if (hasFocus) lingeringRounds = 2;

        _ = ApplyLingeringEffect(casterCreature, target, lingeringDice, lingeringRounds, isUndead,
            eventData.MetaMagicFeat);

        // Epic Focus: contagion spread (does not apply to undead)
        if (!hasEpicFocus || isUndead) return;
        if (target.Location is not { } targetLocation) return;

        int spreadTargets = Math.Max(1, casterCreature.CasterLevel / 10);
        int spreadCount = 0;

        foreach (NwCreature nearby in targetLocation.GetObjectsInShapeByType<NwCreature>(
                     Shape.Sphere, RadiusSize.Medium, true))
        {
            if (spreadCount >= spreadTargets) break;
            if (nearby == target) continue;
            if (!SpellUtils.IsValidHostileTarget(nearby, casterCreature)) continue;
            if (NWScript.GetRacialType(nearby) == NWScript.RACIAL_TYPE_UNDEAD) continue;

            spreadCount++;

            // Tertiary targets only receive the lingering damage, no initial burst, no further spread
            _ = ApplyLingeringEffect(casterCreature, nearby, lingeringDice, lingeringRounds, false,
                eventData.MetaMagicFeat);
        }
    }

    private static async Task ApplyLingeringEffect(NwCreature caster, NwGameObject target, int numDice,
        int rounds, bool isUndead, MetaMagic metaMagic)
    {
        for (int round = 0; round < rounds; round++)
        {
            await NwTask.Delay(NwTimeSpan.FromRounds(1));

            if (target is NwCreature { IsDead: true }) break;

            await caster.WaitForObjectContext();

            int tickDamage = SpellUtils.MaximizeSpell(metaMagic, 3, numDice);
            tickDamage = SpellUtils.EmpowerSpell(metaMagic, tickDamage);

            if (isUndead)
            {
                target.ApplyEffect(EffectDuration.Instant, Effect.Heal(tickDamage));
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpHealingS));
            }
            else
            {
                target.ApplyEffect(EffectDuration.Instant, Effect.Damage(tickDamage, DamageType.Negative));
                target.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpNegativeEnergy));
            }
        }
    }

    #endregion

    #region Shared

    private static void ApplyInflictEffect(NwGameObject target, int amount)
    {
        bool isUndead = NWScript.GetRacialType(target) == NWScript.RACIAL_TYPE_UNDEAD;

        Effect effect = isUndead
            ? Effect.Heal(amount)
            : Effect.Damage(amount, DamageType.Negative);

        Effect vfx = isUndead
            ? Effect.VisualEffect(VfxType.ImpHealingS)
            : Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        target.ApplyEffect(EffectDuration.Instant, effect);
        target.ApplyEffect(EffectDuration.Instant, vfx);
    }

    #endregion
}