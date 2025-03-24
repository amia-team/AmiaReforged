using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Necromancy;

[ServiceBinding(typeof(GhoulTouchOnEnter))]
public class GhoulTouchOnEnter
{
    [ScriptHandler("NW_S0_GhoulTchA")]
    public void OnEnter(CallInfo info)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)) return;
        
        if (eventData.Effect.Creator is not NwCreature caster) return;
        if (eventData.Entering is not NwCreature creature) return;

        bool resistedSpell = caster.SpellAbsorptionLimitedCheck(creature) 
                             || caster.SpellAbsorptionUnlimitedCheck(creature)
                             || caster.SpellImmunityCheck(creature) 
                             || caster.SpellAbsorptionUnlimitedCheck(creature)
                             || caster.SpellResistanceCheck(creature);
        
        if (resistedSpell) return;
        
        ApplyEffect(eventData, caster, creature);
    }
    
    private void ApplyEffect(AreaOfEffectEvents.OnEnter eventData, NwCreature caster, NwCreature enteringCreature)
    {
        if (caster.IsReactionTypeFriendly(enteringCreature)) return;
        
        Effect ghoulVfx = Effect.VisualEffect(VfxType.ImpDoom);
        Effect ghoulEffect = Effect.LinkEffects(Effect.AttackDecrease(2),
            Effect.DamageDecrease(2, DamageType.Piercing | DamageType.Bludgeoning | DamageType.Slashing),
            Effect.SavingThrowDecrease(SavingThrow.All, 2), Effect.SkillDecrease(Skill.AllSkills!, 2));
        
        TimeSpan effectDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 2);

        int dc = SpellUtils.GetAoeSpellDc(eventData);
        
        SavingThrowResult savingThrowResult =
            enteringCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, caster);
        
        if (savingThrowResult == SavingThrowResult.Success)
            enteringCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (savingThrowResult != SavingThrowResult.Failure) return;
        
        enteringCreature.ApplyEffect(EffectDuration.Temporary, ghoulEffect, effectDuration);
        enteringCreature.ApplyEffect(EffectDuration.Instant, ghoulVfx);
    }
}