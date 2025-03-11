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

    public EchoingValleySummonHandler()
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        NwModule.Instance.OnAssociateAdd += OnEchoAdd;
        
        NwCreature? echo = NwObject.FindObjectsWithTag<NwCreature>("summon_echo").FirstOrDefault();
        if (echo != null)
        {
            echo.OnHeartbeat += OnEchoHeartbeat;
        }
        
        Log.Info(message: "Monk Echoing Valley Summon Handler initialized.");
    }
    
    /// <summary>
    /// Since the echo can't be seen, their location and presence is manifested with a vfx
    /// </summary>
    private void OnEchoHeartbeat(CreatureEvents.OnHeartbeat eventData)
    {
        if (eventData.Creature.ResRef is not "summon_echo") return;
        if (eventData.Creature.PlotFlag is false) return;
        
        eventData.Creature.Location?.
            ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDispel, false, 0.4f));
    }

    /// <summary>
    /// Sets the echo undestroyable and hides them
    /// </summary>
    private void OnEchoAdd(OnAssociateAdd eventData)
    {
        if (eventData.Associate.ResRef is not "summon_echo") return;
        
        eventData.Associate.
            ApplyEffect(EffectDuration.Permanent, Effect.VisualEffect(VfxType.DurCutsceneInvisibility));
        eventData.Associate.SetIsDestroyable(false);
    }

}