using Anvil.API;
using Anvil.Services;
using Anvil.API.Events;
using NWN.Core;
using NLog;
using System.Buffers;
using NUnit.Framework.Constraints;

namespace AmiaReforged.Classes.Services;

[ServiceBinding(typeof(WarlockSummonUtilHandler))]
public class WarlockSummonUtilHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public WarlockSummonUtilHandler()
    {
        NwModule.Instance.OnClientLeave += UnsummonOnLeave;
        NwModule.Instance.OnAssociateAdd += UnsummonOnSummon;
        NwModule.Instance.OnPlayerDeath += UnsummonOnDeath;
        NwModule.Instance.OnPlayerRest += UnsummonOnRest;
        NwModule.Instance.OnAssociateRemove += UnsummonOnRemove;
        Log.Info("Warlock Summon Util Handler initialized.");
    }

    [ScriptHandler("wlk_summ_onspawn")]
    public async void OnHenchSpawnSetConditions(CallInfo callInfo)
    {
        if (callInfo.TryGetEvent(out CreatureEvents.OnSpawn obj))
        {
        if (!obj.Creature.ResRef.Contains("wlk")) return;
        if (obj.Creature.AssociateType != AssociateType.Henchman) return;

        NwCreature summon = obj.Creature;
        summon.SetIsDestroyable(true, false, false);

        await NwTask.Delay(TimeSpan.FromSeconds(2.5f));
        summon.GetObjectVariable<LocalVariableInt>("wlk_unsummonable").Value = 1;
        }
    }
    [ScriptHandler("wlk_summ_ondeath")]
    public void OnHenchDeathDestroy(CallInfo callInfo)
    {
        if (callInfo.TryGetEvent(out CreatureEvents.OnDeath obj))
        {
            if (!obj.KilledCreature.ResRef.Contains("wlk")) return;
            if (obj.KilledCreature.AssociateType != AssociateType.Henchman) return;
            
            NwCreature summon = obj.KilledCreature;
            summon.PlayAnimation(Animation.LoopingDeadBack, 1f);
            summon.Destroy();
        }
    }
    [ScriptHandler("wlk_frog_ondeath")]
    public async void OnFrogDeathRussianDoll(CallInfo callInfo)
    {
        if (callInfo.TryGetEvent(out CreatureEvents.OnDeath obj))
        {
            if (obj.KilledCreature.AssociateType != AssociateType.Summoned) return;
            bool isRussianDoll = obj.KilledCreature.ResRef == "wlkslaadblue" ||  obj.KilledCreature.ResRef == "wlkslaadgreen" || 
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
                    summon.Location.ApplyEffect(EffectDuration.Temporary, spawnedSlaad, TimeSpan.FromSeconds(remainingSummonDuration));
                    return;
                }
            }
        }
    }

    private void UnsummonOnLeave(ModuleEvents.OnClientLeave obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Player.ControlledCreature) <= 0) return;
        NWScript.DeleteLocalInt(obj.Player.ControlledCreature, "wlk_summon_cd");
        if (obj.Player.ControlledCreature.GetAssociate(AssociateType.Henchman) == null) return;

        NwCreature warlock = obj.Player.ControlledCreature;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                summon.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                summon.Destroy();
            }
        }
    }
    private void UnsummonOnSummon(OnAssociateAdd obj)
    {
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        if (obj.AssociateType == AssociateType.Dominated) return;

        if (obj.AssociateType == AssociateType.Summoned)
        {
            foreach (NwCreature summon in obj.Owner.Henchmen)
            {
                if (NWScript.GetLocalInt(summon, "wlk_unsummonable") == 1)
                {
                    NWScript.RemoveHenchman(obj.Owner, summon);
                }
            }
        }
        if (obj.Associate.ResRef == "wlkaberrant" || obj.Associate.ResRef == "wlkelemental" || obj.Associate.ResRef == "wlkfiend")
        {
            foreach (NwCreature summon in obj.Owner.Henchmen)
            {
                if (NWScript.GetLocalInt(summon, "wlk_unsummonable") == 1)
                {
                    NWScript.RemoveHenchman(obj.Owner, summon);
                }
            }
            foreach (NwCreature summon in obj.Owner.Associates)
            {
                if (summon.AssociateType == AssociateType.Summoned)
                {
                    summon.Location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpUnsummon));
                    obj.Owner.LoginPlayer.SendServerMessage("Unsummoning " + summon.Name + ".");
                    summon.Destroy();
                }
                return;
            }
        }
    }

    private void UnsummonOnDeath(ModuleEvents.OnPlayerDeath obj)
    {
        if (NWScript.GetLevelByClass(57, obj.DeadPlayer.ControlledCreature) <= 0) return;
        if (obj.DeadPlayer.ControlledCreature.GetAssociate(AssociateType.Henchman) == null) return;

        NwCreature warlock = obj.DeadPlayer.ControlledCreature;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                NWScript.RemoveHenchman(warlock, summon);
            }
        }
    }
    private void UnsummonOnRest(ModuleEvents.OnPlayerRest obj)
    {
        if (obj.RestEventType != RestEventType.Started) return;
        NwCreature warlock = obj.Player.ControlledCreature;

        if (NWScript.GetLevelByClass(57, warlock) <= 0) return;
        if (warlock.GetAssociate(AssociateType.Henchman) == null) return;
        if (NWScript.GetLocalInt(warlock, "AR_RestChoice") == 0) return;

        foreach (NwCreature summon in warlock.Associates)
        {
            if (summon.ResRef == "wlkaberrant" || summon.ResRef == "wlkelemental" || summon.ResRef == "wlkfiend")
            {
                NWScript.RemoveHenchman(warlock, summon);
            }
        }
    }

    private void UnsummonOnRemove(OnAssociateRemove obj)
    {
        if (!obj.Owner.IsPlayerControlled) return;
        if (NWScript.GetLevelByClass(57, obj.Owner) <= 0) return;
        if (obj.Associate.AssociateType != AssociateType.Henchman) return;
        if (!obj.Associate.ResRef.Contains("wlk")) return;

        NwCreature summon = obj.Associate;
        Location summonLocation = summon.Location;

        Effect desummonVfx = summon.ResRef switch
        {
            "wlkaberrant" => Effect.VisualEffect(VfxType.FnfImplosion, false, 0.3f),
            "wlkelemental" => Effect.VisualEffect(VfxType.FnfSummonMonster1),
            "wlkfiend" => Effect.VisualEffect(VfxType.ImpDestruction),
            _ => Effect.VisualEffect(VfxType.ImpUnsummon)
        };
        summonLocation.ApplyEffect(EffectDuration.Instant, desummonVfx);
        summon.Destroy();
        obj.Owner.LoginPlayer.SendServerMessage("Unsummoning "+summon.Name+".");
    }
}
