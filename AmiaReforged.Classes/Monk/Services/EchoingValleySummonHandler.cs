using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(EchoingValleySummonHandler))]
public class EchoingValleySummonHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public EchoingValleySummonHandler(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        NwModule.Instance.OnAssociateAdd += OnEchoAdd;
        // eventService.SubscribeAll<OnAssociateAdd, OnAssociateAdd.Factory>(OnEchoAddAfter, EventCallbackType.After);
        eventService.SubscribeAll<OnEffectApply, OnEffectApply.Factory>(MakeDestroyable, EventCallbackType.After);
        NwModule.Instance.OnEffectApply += OnEchoAwakened;
        
        Log.Info(message: "Monk Echoing Valley Summon Handler initialized.");
    }

    /// <summary>
    /// Since the echo can't be seen, their location and presence is manifested with a vfx
    /// </summary>
    [ScriptHandler("nw_ch_ac1")]
    private void OnEchoHeartbeat(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature echo) return;
        if (echo.ResRef is not "summon_echo") return;
        if (echo.PlotFlag is false) return;
        
        echo.Location?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDispel, false, 0.4f));
    }
    
    /// <summary>
    /// In combat deal a silly little 1d6 dmg per round in a medium AOE
    /// </summary>
    [ScriptHandler("nw_ch_ac3")]
    private void OnEchoCombatRoundEnd(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature echo) return;
        if (echo.ResRef is not "summon_echo") return;

        Effect echoDamageVfx = MonkUtilFunctions.ResizedVfx(VfxType.ImpBlindDeafM, RadiusSize.Medium);
        Effect echoDamage = Effect.Damage(Random.Shared.Roll(6), DamageType.Sonic);
        Effect echoEffect = Effect.LinkEffects(echoDamage, echoDamageVfx);
        
        echo.Location?.ApplyEffect(EffectDuration.Instant, echoEffect);

        if (echo.Master is null) return;

        echo.ActionForceMoveTo(echo.Master, true);
    }
    
    /// <summary>
    /// When awakened by monk's Ki Shout, is able to die and explodes in 10d6 sonic dmg
    /// </summary>
    /// <param name="info"></param>
    [ScriptHandler("nw_ch_ac7")]
    private void OnEchoDeath(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature echo) return;
        if (echo.ResRef is not "summon_echo") return;

        Effect echoDamageVfx = MonkUtilFunctions.ResizedVfx(VfxType.ImpBlindDeafM, RadiusSize.Large);
        Effect echoDamage = Effect.Damage(Random.Shared.Roll(6, 10), DamageType.Sonic);
        Effect echoEffect = Effect.LinkEffects(echoDamage, echoDamageVfx);
        
        echo.Location?.ApplyEffect(EffectDuration.Instant, echoEffect);
    }

    /// <summary>
    /// Sets the old echoes undestroyable and hides new echoes from view
    /// </summary>
    private void OnEchoAdd(OnAssociateAdd eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;

        NwCreature monk = eventData.Owner;
        
        // Hides the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, monk);
        
        Effect echoEffect = Effect.LinkEffects(Effect.Pacified(), Effect.Ethereal());
        echoEffect.Tag = "summonecho_effect";
        echoEffect.SubType = EffectSubType.Unyielding;
        
        eventData.Associate.ApplyEffect(EffectDuration.Permanent, echoEffect);
        
        foreach (NwCreature associate in monk.Associates)
            if (associate.Tag == "summon_echo")
                associate.IsDestroyable = false;
    }
    
    /// <summary>
    /// Sets the echoes destroyable again
    /// </summary>
    private void OnEchoAddAfter(OnAssociateAdd eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;

        NwCreature monk = eventData.Owner;
        
        // Shows the stupid "unsummoning creature" message again
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, monk);
        
        foreach (NwCreature associate in monk.Associates)
            if (associate.Tag == "summon_echo")
                associate.IsDestroyable = true;
    }
    
    private void MakeDestroyable(OnEffectApply eventData)
    {
        if (eventData.Object.ResRef is not "summon_echo") return;
        if (eventData.Effect.Tag is not "summonecho_effect") return;

        NwCreature echo = (NwCreature)eventData.Object;

        if (echo.Master is null) return;
        
        NwCreature monk = echo.Master;
        
        // Shows the stupid "unsummoning creature" message again
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, monk);
        
        foreach (NwCreature associate in monk.Associates)
            if (associate.Tag == "summon_echo")
                associate.IsDestroyable = true;
    }
    
    /// <summary>
    /// Removes the protective plot flag and ethereal and pacified effects so they can die, commands echoes to attack
    /// </summary>
    private void OnEchoAwakened(OnEffectApply eventData)
    {
        if (eventData.Object.ResRef is not "summon_echo") return;
        if (eventData.Effect.Tag is not "kishout_echoingvalley") return;

        NwCreature echo = (NwCreature)eventData.Object;
        
        foreach (Effect effect in echo.ActiveEffects)
            if  (effect.Tag == "summonecho_effect")
                echo.RemoveEffect(effect);
        
        NwCreature nearestHostile =
            echo.GetNearestCreatures().First(creature => creature.IsReactionTypeHostile(echo));
        echo.ActionAttackTarget(nearestHostile);
    }

}