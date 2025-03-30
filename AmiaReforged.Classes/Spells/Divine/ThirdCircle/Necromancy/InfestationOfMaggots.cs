using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.ThirdCircle.Necromancy;

[ServiceBinding(typeof(ISpell))]
public class InfestationOfMaggots : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "X2_S0_InfestMag";
    
    private int _spellDc;
    private void SetMaggotsDc(int spellDc)
    {
        _spellDc = spellDc;
    }
    private int GetMaggotsDc()
    {
        return _spellDc;
    }
    
    private NwCreature? _spellTarget;
    private void SetMaggotsTarget(NwCreature spellTarget)
    {
        _spellTarget = spellTarget;
    }
    private NwCreature? GetMaggotsTarget()
    {
        return _spellTarget;
    }
    
    private NwCreature? _spellCaster;
    private void SetMaggotsCaster(NwCreature spellCaster)
    {
        _spellCaster = spellCaster;
    }
    private NwCreature? GetMaggotsCaster()
    {
        return _spellCaster;
    }
    
    private int _conDamage;
    private void SetMaggotsConDamage(int conDamage)
    {
        _conDamage = conDamage;
    }
    private int GetMaggotsConDamage()
    {
        return _conDamage;
    }
    
    private ScriptHandleFactory ScriptHandleFactory { get; }
    private SchedulerService SchedulerService { get; }

    public InfestationOfMaggots(ScriptHandleFactory scriptHandleFactory, SchedulerService schedulerService)
    {
        ScriptHandleFactory = scriptHandleFactory;
        SchedulerService = schedulerService;
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature caster) return;

        if (eventData.TargetObject is not NwCreature targetCreature) return;

        if (caster.IsReactionTypeFriendly(targetCreature)) return;

        if (ResistedSpell) return;
        
        SpellUtils.SignalSpell(caster, targetCreature, eventData.Spell);
        
        int dc = SpellUtils.GetSpellDc(eventData);

        if (targetCreature.ActiveEffects.Any(effect => effect.Tag == "infestation_of_maggots_effect")) return;

        SavingThrowResult saveResult = targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, 
            SavingThrowType.Disease, caster);
        
        if (saveResult == SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (saveResult != SavingThrowResult.Failure) return;
        
        SetMaggotsDc(dc);
        SetMaggotsTarget(targetCreature);
        SetMaggotsCaster(targetCreature);
        
        ApplyMaggotsEffect(caster, targetCreature, eventData.MetaMagicFeat);
    }

    private void ApplyMaggotsEffect(NwCreature caster, NwCreature targetCreature, MetaMagic metaMagic)
    {
        ScriptCallbackHandle maggotsIntervalHandle = ScriptHandleFactory.CreateUniqueHandler(OnMaggotsInterval);
        
        Effect maggotsEffect = Effect.RunAction(
            onIntervalHandle: maggotsIntervalHandle, 
            interval: NwTimeSpan.FromRounds(1));

        Effect maggotsVfx = Effect.VisualEffect(VfxType.DurFlies);

        maggotsEffect = Effect.LinkEffects(maggotsEffect, maggotsVfx);
        
        maggotsEffect.SubType = EffectSubType.Extraordinary;
        maggotsEffect.Tag = "infestation_of_maggots_effect";

        TimeSpan effectDuration = NwTimeSpan.FromRounds(10 + caster.CasterLevel);
        effectDuration = SpellUtils.CheckExtend(metaMagic, effectDuration);

        int conDamage = SpellUtils.CheckMaximize(metaMagic, 4, 1);
        conDamage = SpellUtils.CheckEmpower(metaMagic, conDamage);
        
        SetMaggotsConDamage(conDamage);
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, maggotsEffect, effectDuration);
        ApplyConDamage(targetCreature, conDamage);
        
        SchedulerService.Schedule(() =>
        {
            maggotsIntervalHandle.Dispose();
        }, effectDuration + NwTimeSpan.FromRounds(1)); // Small grace period to allow for inconsistent game updates.
    }

    private static void ApplyConDamage(NwCreature targetCreature, int conDamage)
    {
        Effect impactVfx = Effect.VisualEffect(VfxType.ImpDiseaseS);
        
        // Check for prior con damage; if prior con damage is found, add the maggots con damage on top of it 
        Effect? priorConDamage = targetCreature.ActiveEffects.FirstOrDefault(effect => 
            effect.EffectType == EffectType.AbilityDecrease
            && effect.IntParams[0] == (int)Ability.Constitution);
        
        if (priorConDamage != null)
            conDamage += priorConDamage.IntParams[1];

        Effect conDamageEffect = Effect.AbilityDecrease(Ability.Constitution, conDamage);
        conDamageEffect.SubType = EffectSubType.Extraordinary;
        
        targetCreature.ApplyEffect(EffectDuration.Instant, impactVfx);
        targetCreature.ApplyEffect(EffectDuration.Permanent, conDamageEffect);
    }

    private ScriptHandleResult OnMaggotsInterval(CallInfo info)
    {
        NwCreature? maggotsTarget = GetMaggotsTarget();
        if (maggotsTarget == null) return ScriptHandleResult.Handled;
        
        NwCreature? maggotsCaster = GetMaggotsCaster();
        int dc = GetMaggotsDc();
        
        SavingThrowResult saveResult = maggotsCaster != null ? 
            maggotsTarget.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Disease, maggotsCaster) 
            : maggotsTarget.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Disease);
        
        if (saveResult == SavingThrowResult.Success)
            maggotsTarget.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (saveResult != SavingThrowResult.Failure)
        {
            foreach (Effect effect in maggotsTarget.ActiveEffects)
                if (effect.Tag == "infestation_of_maggots_effect")
                    maggotsTarget.RemoveEffect(effect);
            
            return ScriptHandleResult.Handled;
        }
        
        int conDamage = GetMaggotsConDamage();
        
        ApplyConDamage(maggotsTarget, conDamage);
        
        return ScriptHandleResult.Handled;
    }


    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}