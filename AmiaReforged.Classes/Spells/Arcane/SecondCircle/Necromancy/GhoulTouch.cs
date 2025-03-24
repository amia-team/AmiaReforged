using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Necromancy;

[ServiceBinding(typeof(ISpell))]
public class GhoulTouch : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "NW_S0_GhoulTch";
    

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;
        if (eventData.TargetObject is null) return;
        
        SpellUtils.SignalSpell(casterCreature, eventData.TargetObject, eventData.Spell);
        
        if (ResistedSpell) return;

        ApplyEffect(eventData);
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
    
    private static void ApplyEffect(SpellEvents.OnSpellCast eventData)
    {
        Effect fogGhoul = Effect.AreaOfEffect(PersistentVfxType.PerFogghoul!);
        
        Effect ghoulTouchEffect = Effect.LinkEffects(Effect.Paralyze(), 
            Effect.VisualEffect(VfxType.DurCessateNegative), Effect.VisualEffect(VfxType.DurParalyzed));
        
        TimeSpan effectDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 2);
        effectDuration = SpellUtils.CheckExtend(eventData.MetaMagicFeat, effectDuration);

        int dc = SpellUtils.GetSpellDc(eventData);
        
        NwCreature targetCreature = (NwCreature)eventData.TargetObject!;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, eventData.Caster);
        
        if (savingThrowResult == SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (savingThrowResult != SavingThrowResult.Failure) return;
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, ghoulTouchEffect, effectDuration);
        targetCreature.Location!.ApplyEffect(EffectDuration.Temporary, fogGhoul, effectDuration);
    }
}