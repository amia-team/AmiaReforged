using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Necromancy;

public class GhoulTouchOnEnter
{
    [ScriptHandler("NW_S0_GhoulTchA")]
    public static void OnScriptRun(CallInfo info)
    {
        AreaOfEffectEvents.OnEnter eventData = new();
        
        eventData.Effect.OnEnter += OnEnterGhoul;
    }
    
    private static void OnEnterGhoul(AreaOfEffectEvents.OnEnter eventData)
    {
        if (eventData.Effect.Creator is not NwCreature caster) return;
        if (eventData.Entering is not NwCreature enteringCreature) return;
        
        bool resistedSpell = enteringCreature.SpellAbsorptionLimitedCheck(caster) 
                             || enteringCreature.SpellAbsorptionUnlimitedCheck(caster)
                             || enteringCreature.SpellImmunityCheck(caster) 
                             || enteringCreature.SpellAbsorptionUnlimitedCheck(caster)
                             || enteringCreature.SpellResistanceCheck(caster);
        
        if (resistedSpell) return;
        
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