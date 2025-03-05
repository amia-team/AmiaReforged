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

    private bool _hasFocus;
    private bool _hasGreaterFocus;
    private bool _hasEpicFocus;

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

        _hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        _hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        _hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int damage = CalculateDamage(casterCreature);

        if (Result != ResistSpellResult.Failed || !skipTouchAttack) return;

        ApplyEffect(eventData, damage);
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

    private int CalculateDamage(NwCreature casterCreature)
    {
        int bonusDie = _hasFocus ? 1 : _hasGreaterFocus ? 2 : _hasEpicFocus ? 3 : 0;

        int numDie = casterCreature.CasterLevel / 2 + bonusDie;

        return NWScript.d3(numDie);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }
}