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
    private int _spellDc;
    private void SetGhoulDc(int spellDc)
    {
        _spellDc = spellDc;
    }
    private int GetGhoulDc()
    {
        return _spellDc;
    }

    private SchedulerService SchedulerService { get; }
    private ScriptHandleFactory ScriptHandleFactory { get; }
    public GhoulTouch(ScriptHandleFactory scriptHandleFactory, SchedulerService schedulerService)
    {
        ScriptHandleFactory = scriptHandleFactory;
        SchedulerService = schedulerService;
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
        AreaOfEffectEvents.OnEnter eventData = new();

        if (eventData.Entering is not NwCreature enteringCreature) return ScriptHandleResult.Handled;
        enteringCreature.SpeakString("Teeheee I stepped on your goo");
        if (eventData.Effect.Creator is not NwCreature caster) return ScriptHandleResult.Handled;
        caster.SpeakString("Oh no I stepped on my own goo!");
        
        if (caster.IsReactionTypeFriendly(enteringCreature)) return ScriptHandleResult.Handled;
        enteringCreature.SpeakString("Still gooey after friendly check");
        
        if (ResistedSpell) return ScriptHandleResult.Handled;
        enteringCreature.SpeakString("Yep, gooey after spell resist check");
        
        int dc = GetGhoulDc();
        enteringCreature.SpeakString($"Gooey after getting goo DC! DC is {dc}");
        
        Effect ghoulVfx = Effect.VisualEffect(VfxType.ImpDoom);
        Effect ghoulEffect = Effect.LinkEffects(Effect.AttackDecrease(2),
            Effect.DamageDecrease(2),
            Effect.SavingThrowDecrease(SavingThrow.All, 2), Effect.SkillDecrease(Skill.AllSkills!, 2));
        
        enteringCreature.SpeakString("Declared effects, still gooey.");
        
        TimeSpan effectDuration = NwTimeSpan.FromRounds(Random.Shared.Roll(6) + 2);
        
        enteringCreature.SpeakString("Duration set, gooing strong.");
        
        SavingThrowResult savingThrowResult =
            enteringCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, caster);
        
        enteringCreature.SpeakString("Rolled fort against your mighty goo.");

        if (savingThrowResult == SavingThrowResult.Success)
        {
            enteringCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            enteringCreature.SpeakString("Hah, your goo is weak.");
        }

        if (savingThrowResult != SavingThrowResult.Failure) return ScriptHandleResult.Handled;
        
        enteringCreature.ApplyEffect(EffectDuration.Temporary, ghoulEffect, effectDuration);
        enteringCreature.ApplyEffect(EffectDuration.Instant, ghoulVfx);
        
        enteringCreature.SpeakString("Your goo is too powerful!!!");

        return ScriptHandleResult.Handled;
    }
}