using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.FourthCircle.Necromancy;

[ServiceBinding(typeof(ISpell))]
public class Enervation : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_Enervat";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        if (eventData.TargetObject is not NwCreature targetCreature) return;

        if (caster.IsReactionTypeFriendly(targetCreature)) return;

        if (ResistedSpell) return;
        
        SpellUtils.SignalSpell(caster, targetCreature, eventData.Spell);
        
        int dc = SpellUtils.GetSpellDc(eventData);
        
        SavingThrowResult saveResult = targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, 
            SavingThrowType.Negative, caster);
        
        if (saveResult == SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (saveResult != SavingThrowResult.Failure) return;
        
        ApplyEffect(caster, targetCreature, eventData.MetaMagicFeat);
    }

    private static void ApplyEffect(NwCreature caster, NwCreature targetCreature, MetaMagic metaMagic)
    {
        Effect impactVfx = Effect.VisualEffect(VfxType.ImpReduceAbilityScore);
        
        int levelDrain = SpellUtils.MaximizeSpell(metaMagic, 4, 1);
        levelDrain = SpellUtils.EmpowerSpell(metaMagic, levelDrain);

        Effect enervationEffect = Effect.LinkEffects(
            Effect.VisualEffect(VfxType.DurCessateNegative), 
            Effect.NegativeLevel(levelDrain));

        TimeSpan effectDuration = NwTimeSpan.FromHours(caster.CasterLevel);
        effectDuration = SpellUtils.ExtendSpell(metaMagic, effectDuration);
        
        
        targetCreature.ApplyEffect(EffectDuration.Instant, impactVfx);
        targetCreature.ApplyEffect(EffectDuration.Temporary, enervationEffect, effectDuration);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}