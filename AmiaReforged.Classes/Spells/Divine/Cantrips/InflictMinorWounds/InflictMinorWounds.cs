using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.InflictMinorWounds;

[ServiceBinding(typeof(ISpell))]
public class InflictMinorWounds : ISpell
{
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X0_S0_Inflict";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        ResistedSpell = creature.SpellResistanceCheck(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        TouchAttackResult result = casterCreature.TouchAttackRanged(eventData.TargetObject, true);

        bool skipTouchAttack = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD;

        if (result != TouchAttackResult.Hit || !skipTouchAttack) return;

        int damage = CalculateDamage(casterCreature);

        if (!ResistedSpell || !skipTouchAttack) return;

        ApplyEffect(eventData, damage);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private int CalculateDamage(NwCreature casterCreature)
    {
        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int bonusDie = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;

        int numDie = casterCreature.CasterLevel / 2 + bonusDie;

        return NWScript.d3(numDie);
    }

    private static void ApplyEffect(SpellEvents.OnSpellCast eventData, int damage)
    {
        Effect effectBasedOnType = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD
            ? Effect.Heal(damage)
            : Effect.Damage(damage, DamageType.Negative);

        Effect vfx = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD
            ? Effect.VisualEffect(VfxType.ImpHealingS)
            : Effect.VisualEffect(VfxType.ImpNegativeEnergy);

        eventData.TargetObject!.ApplyEffect(EffectDuration.Instant, effectBasedOnType);
        eventData.TargetObject!.ApplyEffect(EffectDuration.Instant, vfx);
    }
}