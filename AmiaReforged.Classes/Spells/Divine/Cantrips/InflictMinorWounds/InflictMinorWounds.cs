using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.InflictMinorWounds;

[ServiceBinding(typeof(ISpell))]
public class InflictMinorWounds : ISpell
{
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "X0_S0_Inflict";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject == null) return;

        Task<TouchAttackResult> result = casterCreature.TouchAttackRanged(eventData.TargetObject, true);

        bool skipTouchAttack = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD;

        if (result.Result != TouchAttackResult.Hit || !skipTouchAttack) return;

        int damage = CalculateDamage(casterCreature);

        if (Result != ResistSpellResult.Failed || !skipTouchAttack) return;

        ApplyEffect(eventData, damage);
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

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}