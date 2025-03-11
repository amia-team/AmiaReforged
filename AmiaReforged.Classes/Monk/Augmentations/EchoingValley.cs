using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
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
            // Create the new echo and attach it as a summon
            Location? summonLocation = SummonUtility.GetRandomLocationAroundPoint(monk.Location!, 4f);
            Effect summonEcho = Effect.SummonCreature("echo", VfxType.FnfPwstun);
            TimeSpan summonDuration = NwTimeSpan.FromTurns(2);

            await monk.WaitForObjectContext();
            summonLocation?.ApplyEffect(EffectDuration.Temporary,summonEcho, summonDuration);
        }
    }
    
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
    
    private static void AugmentKiShout(OnSpellCast castData)
    {
    }

    private static void AugmentQuiveringPalm(OnSpellCast castData)
    {
    }
}