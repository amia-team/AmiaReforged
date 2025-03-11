using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Constants;
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
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
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
        
        if (attackData.Target is not NwCreature targetCreature) return;
        
        NwCreature monk = attackData.Attacker;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int echoCap = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        
        // Check how many echoes monk has
        int echoCount = monk.Associates.Count(associate => 
            associate is { AssociateType: AssociateType.Summoned, Tag: "stunningstrike_echoingvalley" });
        
        // Return if capped out
        if (echoCount <= echoCap) return;

        SummonEcho();

        return;
        
        async void SummonEcho()
        {
            Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location!, 4f);
            
            if (summonLocation is null) return;
            
            Effect summonEcho = Effect.SummonCreature("echo_echoingvalley", VfxType.ImpMagicProtection);
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
            associate is { AssociateType: AssociateType.Summoned, Tag: "stunningstrike_echoingvalley" });

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
            if (!monk.Associates.Contains(nwObject) || nwObject.ResRef != "echo_echoingvalley") continue;
            
            nwObject.Location?.ApplyEffect(EffectDuration.Instant, kiShoutEffect);
        }
    }
    
    /// <summary>
    /// Quivering Palm creates an Echo of the targeted creature to fight alongside the monk for one turn.
    /// </summary>
    private static void AugmentQuiveringPalm(OnSpellCast castData)
    {
        QuiveringPalm.DoQuiveringPalm(castData);
        
        if (castData.TargetObject is not NwCreature targetCreature) return;
        
        NwCreature monk = (NwCreature)castData.Caster;
        
        TouchAttackResult touchAttackResult = monk.TouchAttackMelee(targetCreature).Result;

        if (touchAttackResult == TouchAttackResult.Miss) return;

        SummonClone();

        return;

        async void SummonClone()
        {
            // Create the new echo and attach it as a summon
            Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location!, 4f);
            
            if (summonLocation is null) return;
            
            Effect summonEcho = Effect.SummonCreature(targetCreature.ResRef, VfxType.FnfPwstun);
            TimeSpan summonDuration = NwTimeSpan.FromTurns(1);

            await monk.WaitForObjectContext();

            summonLocation.ApplyEffect(EffectDuration.Temporary, summonEcho, summonDuration);
        }
    }
}