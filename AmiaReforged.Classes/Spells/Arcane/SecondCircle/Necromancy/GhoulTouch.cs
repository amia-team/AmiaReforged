using System.Runtime.CompilerServices;
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
    [Inject]
    private ScriptHandleFactory ScriptHandleFactory { get; init; }
    private SchedulerService SchedulerService { get; }

    private int _spellDc;
    private void SetGhoulDc(int spellDc)
    {
        _spellDc = spellDc;
    }
    private int GetGhoulDc()
    {
        return _spellDc;
    }

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

    private void ApplyEffect(SpellEvents.OnSpellCast eventData)
    {
        Effect ghoulTouchEffect = Effect.LinkEffects(Effect.Paralyze(), 
            Effect.VisualEffect(VfxType.DurCessateNegative), Effect.VisualEffect(VfxType.DurParalyzed));
        
        TimeSpan effectDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 2);
        effectDuration = SpellUtils.CheckExtend(eventData.MetaMagicFeat, effectDuration);

        int dc = SpellUtils.GetSpellDc(eventData);
        SetGhoulDc(dc);
        
        NwCreature targetCreature = (NwCreature)eventData.TargetObject!;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, eventData.Caster);
        
        if (savingThrowResult == SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (savingThrowResult != SavingThrowResult.Failure) return;
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, ghoulTouchEffect, effectDuration);
        
        ScriptCallbackHandle ghoulAoeEnterHandle = ScriptHandleFactory.CreateUniqueHandler(OnEnterGhoulTouch);

        Effect fogGhoul = Effect.AreaOfEffect(PersistentVfxType.PerFogghoul!, ghoulAoeEnterHandle);
        
        targetCreature.Location!.ApplyEffect(EffectDuration.Temporary, fogGhoul, effectDuration);
        
        SchedulerService.Schedule(() =>
        {
            ghoulAoeEnterHandle.Dispose();
        }, effectDuration + NwTimeSpan.FromRounds(1)); // Small grace period to allow for inconsistent game updates.
    }

    private ScriptHandleResult OnEnterGhoulTouch(CallInfo info)
    {
        if (info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData) == false) return ScriptHandleResult.Handled;

        if (eventData.Entering is not NwCreature enteringCreature) return ScriptHandleResult.Handled;
        if (eventData.Effect.Creator is not NwCreature caster) return ScriptHandleResult.Handled;
        
        if (caster.IsReactionTypeFriendly(enteringCreature)) return ScriptHandleResult.Handled;
        
        if (ResistedSpell) return ScriptHandleResult.Handled;
        
        int dc = GetGhoulDc();
        
        Effect ghoulVfx = Effect.VisualEffect(VfxType.ImpDoom);
        Effect ghoulEffect = Effect.LinkEffects(Effect.AttackDecrease(2),
            Effect.DamageDecrease(2,  DamageType.Slashing),
            Effect.SavingThrowDecrease(SavingThrow.All, 2), Effect.SkillDecrease(Skill.AllSkills!, 2));
        
        TimeSpan effectDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 2);
        
        SavingThrowResult savingThrowResult =
            enteringCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, caster);
        
        if (savingThrowResult == SavingThrowResult.Success)
            enteringCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (savingThrowResult != SavingThrowResult.Failure) return ScriptHandleResult.Handled;
        
        enteringCreature.ApplyEffect(EffectDuration.Temporary, ghoulEffect, effectDuration);
        enteringCreature.ApplyEffect(EffectDuration.Instant, ghoulVfx);
        
        return ScriptHandleResult.Handled;
    }
}