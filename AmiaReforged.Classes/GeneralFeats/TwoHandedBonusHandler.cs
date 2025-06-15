using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(TwoHandedBonusHandler))]
public class TwoHandedBonusHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TwoHandedBonusHandler(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;
        
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyTwoHanded, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyTwoHanded, EventCallbackType.After);
        NwModule.Instance.OnEffectApply += OnStrengthGainApplyTwoHanded;
        NwModule.Instance.OnEffectRemove += OnStrengthRemoveApplyTwoHanded;
        Log.Info(message: "Monk Static Bonuses Service initialized.");
    }
    
    private static void OnEquipApplyTwoHanded(OnItemEquip eventData)
    {
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.EquippedBy);
    }
    
    private static void OnUnequipApplyTwoHanded(OnItemUnequip eventData)
    {
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.Creature);
    }

    private static void OnStrengthGainApplyTwoHanded(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Strength) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(creature);
    }

    private static void OnStrengthRemoveApplyTwoHanded(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Strength) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(creature);
    }
    
    
    
}