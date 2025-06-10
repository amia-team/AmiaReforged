using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockSummonUtilHandler))]
public class WarlockSummonUtilHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockSummonUtilHandler()
    {
        NwModule.Instance.OnAssociateAdd += AllowMultipleSummons;
        Log.Info(message: "Warlock Summon Util Handler initialized.");
    }
    
    /// <summary>
    /// Allows multiple warlock summons
    /// </summary>
    private static void AllowMultipleSummons(OnAssociateAdd eventData)
    {
        if (!eventData.Associate.ResRef.Contains("wlk")) return;

        NwCreature warlock = eventData.Owner;
        
        // Hides the stupid "unsummoning creature" message
        FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 1, warlock);
        
        foreach (NwCreature associate in warlock.Associates)
            if (associate.ResRef.Contains("wlk"))
                associate.IsDestroyable = false;
        
        DelayedMakeDestroyable();
        return;
        
        async void DelayedMakeDestroyable()
        {
            await NwTask.Delay(TimeSpan.FromSeconds(6));
        
            foreach (NwCreature associate in warlock.Associates)
                if (associate.ResRef.Contains("wlk"))
                    associate.IsDestroyable = true;
            
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackPlugin.NWNX_FEEDBACK_ASSOCIATE_UNSUMMONING, 0, warlock);
        }
    }
    
    [ScriptHandler(scriptName: "wlk_frog_ondeath")]
    public async void OnFrogDeathRussianDoll(CallInfo callInfo)
    {
        if (callInfo.TryGetEvent(out CreatureEvents.OnDeath obj))
        {
            if (obj.KilledCreature.AssociateType != AssociateType.Summoned) return;
            bool isRussianDoll = obj.KilledCreature.ResRef == "wlkslaadblue" ||
                                 obj.KilledCreature.ResRef == "wlkslaadgreen" ||
                                 obj.KilledCreature.ResRef == "wlkslaadgray";
            if (!isRussianDoll) return;

            NwCreature summon = obj.KilledCreature;
            NwCreature warlock = summon.Master;

            string slaadTier = summon.ResRef;

            // Get the summon duration from warlock's active effects
            foreach (Effect effect in warlock.ActiveEffects)
            {
                if (effect.Tag == "frogduration")
                {
                    float remainingSummonDuration = effect.DurationRemaining;

                    slaadTier = slaadTier switch
                    {
                        "wlkslaadblue" => "wlkslaadred",
                        "wlkslaadgreen" => "wlkslaadblue",
                        "wlkslaadgray" => "wlkslaadgreen",
                        _ => "wlkslaadred"
                    };

                    await NwTask.Delay(TimeSpan.FromSeconds(2));
                    await warlock.WaitForObjectContext();
                    Effect spawnedSlaad = Effect.SummonCreature(slaadTier, VfxType.ImpPolymorph);
                    summon.Location?.ApplyEffect(EffectDuration.Temporary, spawnedSlaad,
                        TimeSpan.FromSeconds(remainingSummonDuration));
                    return;
                }
            }
        }
    }
    
}