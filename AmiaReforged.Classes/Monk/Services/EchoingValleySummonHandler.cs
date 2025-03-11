using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
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
        NwModule.Instance.OnAssociateRemove += OnEchoRemove;
        
        Log.Info(message: "Monk Echoing Valley Summon Handler initialized.");
    }

    /// <summary>
    /// Since the echo can't be seen, their location and presence is manifested with a vfx
    /// </summary>
    [ScriptHandler("nw_ch_ac1")]
    private static void OnEchoHeartbeat(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature echo) return;
        if (echo.ResRef is not "summon_echo") return;
        if (echo.PlotFlag is false) return;
        
        echo.Location?.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDispel, false, 0.4f));
    }

    /// <summary>
    /// Hides the new echo and sets old echoes undestroyable so they're not unsummoned
    /// </summary>
    private static void OnEchoAdd(OnAssociateAdd eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;

        NwCreature monk = eventData.Owner;
        
        eventData.Associate.
            ApplyEffect(EffectDuration.Permanent, Effect.VisualEffect(VfxType.DurCutsceneInvisibility));
        
        StopEchoUnsummoning();
        
        return;
        
        async void StopEchoUnsummoning()
        {
            foreach (NwCreature associate in monk.Associates)
                if (associate.ResRef is "summon_echo")
                    await associate.SetIsDestroyable(false);

            await NwTask.Delay(TimeSpan.FromMilliseconds(1));

            if (!monk.IsValid) return;
        
            foreach (NwCreature associate in monk.Associates)
                if (associate.ResRef is "summon_echo")
                    await associate.SetIsDestroyable(true);
        }
    }
    
    private static void OnEchoRemove(OnAssociateRemove eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;
        
    }
    

}