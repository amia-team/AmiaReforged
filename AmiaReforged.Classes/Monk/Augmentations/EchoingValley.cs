using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class EchoingValley
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunningStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.Quivering:
                AugmentQuiveringPalm(castData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
            case TechniqueType.Wholeness:
                WholenessOfBody.DoWholenessOfBody(castData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
        }
    }
    
    /// <summary>
    /// Stunning Strike summons an Echo to empower Stunning Strike with +1d4 bonus magical damage.
    /// Echoes last for two turns. Each Ki Focus allows an additional Echo to be summoned for an additional
    /// 1d4 bonus magical damage to a maximum of 4d4.
    /// </summary>
    private static void AugmentStunningStrike(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);
        
        NwCreature monk = attackData.Attacker;
        
        if (attackData.Target is not NwCreature targetCreature) return;
        if (!targetCreature.IsReactionTypeHostile(monk)) return;
        
        int echoCap = MonkUtilFunctions.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };
        
        // Check how many echoes monk has
        int echoCount = monk.Associates.Count(associate => 
            associate is { AssociateType: AssociateType.Summoned, ResRef: "summon_echo" });
        
        // Return if capped out
        if (echoCount == echoCap) return;

        SummonEcho();

        return;
        
        async void SummonEcho()
        {
            Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location!, 3f);
            
            if (summonLocation is null) return;
            
            Effect summonEcho = Effect.SummonCreature("summon_echo", VfxType.ImpMagicProtection!, 
                TimeSpan.FromSeconds(1), 0, VfxType.ImpGrease);
            TimeSpan summonDuration = NwTimeSpan.FromTurns(2);

            await monk.WaitForObjectContext();
            summonLocation.ApplyEffect(EffectDuration.Temporary, summonEcho, summonDuration);
        }
    }
    
    /// <summary>
    /// Empty Body grants +1 bonus dodge AC for each Echo.
    /// </summary>
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        EmptyBody.DoEmptyBody(castData);
        
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        // Check how many echoes monk has
        int echoCount = monk.Associates.Count(associate => 
            associate is { AssociateType: AssociateType.Summoned, ResRef: "summon_echo" });

        Effect emptyBodyEffect = Effect.LinkEffects(Effect.ACIncrease(echoCount), 
            Effect.VisualEffect(VfxType.DurPdkFear));
        emptyBodyEffect.Tag = "emptybody_echoingvalley";
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);

        foreach (Effect effect in monk.ActiveEffects)
            if (effect.Tag == "emptybody_echoingvalley")
                monk.RemoveEffect(effect);
        
        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
    }
    
    /// <summary>
    /// Ki Shout awakens the Echoes, granting them the ability to fight.
    /// </summary>
    private static void AugmentKiShout(OnSpellCast castData)
    {
        KiShout.DoKiShout(castData);

        NwCreature monk = (NwCreature)castData.Caster;

        Effect kiShoutEffect = Effect.VisualEffect(VfxType.FnfPwstun, false, 0.7f);
        kiShoutEffect.Tag = "kishout_echoingvalley";

        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {   
            // Must be echo and monk's associate
            if (!monk.Associates.Contains(nwObject) || nwObject.ResRef != "summon_echo") continue;
            
            nwObject.Location?.ApplyEffect(EffectDuration.Instant, kiShoutEffect);
        }
    }
    
    /// <summary>
    /// Quivering Palm creates an Echo of the targeted creature to fight alongside the monk for one turn.
    /// </summary>
    private static void AugmentQuiveringPalm(OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(castData);
        
        if (castData.TargetObject is not NwCreature targetCreature) return;
        if (touchAttackResult == TouchAttackResult.Miss) return;
        
        NwCreature monk = (NwCreature)castData.Caster;

        SummonClone();

        return;

        async void SummonClone()
        {
            Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location!, 4f);
            
            if (summonLocation is null) return;
            
            Effect summonClone = Effect.SummonCreature(targetCreature, VfxType.FnfPwstun!);
            TimeSpan summonDuration = NwTimeSpan.FromTurns(1);

            await monk.WaitForObjectContext();
            summonLocation.ApplyEffect(EffectDuration.Temporary, summonClone, summonDuration);
        }
    }
}