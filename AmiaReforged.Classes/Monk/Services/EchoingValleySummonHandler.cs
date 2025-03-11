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
        eventService.SubscribeAll<OnAssociateAdd, OnAssociateAdd.Factory>(OnEchoAddAfter, EventCallbackType.After);
        
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
    /// Sets the old echoes undestroyable and hides new echoes from view
    /// </summary>
    private void OnEchoAdd(OnAssociateAdd eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;

        NwCreature monk = eventData.Owner;
        
        // Hides the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, monk);
        
        Effect echoEffect = Effect.LinkEffects(Effect.VisualEffect(VfxType.DurCutsceneInvisibility), Effect.Pacified(),
            Effect.Ethereal());
        echoEffect.Tag = "summonecho_effect";
        echoEffect.SubType = EffectSubType.Unyielding;
        
        eventData.Associate.
            ApplyEffect(EffectDuration.Permanent, Effect.VisualEffect(VfxType.DurCutsceneInvisibility));
        
        foreach (NwCreature associate in monk.Associates)
            if (associate.Tag == "summon_echo")
                associate.SetIsDestroyable(false);
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
                associate.SetIsDestroyable(true);
    }

}