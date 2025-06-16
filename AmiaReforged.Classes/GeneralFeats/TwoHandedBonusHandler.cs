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
        NwModule.Instance.OnEffectApply += OnStrengthApplyApplyTwoHanded;
        NwModule.Instance.OnEffectRemove += OnStrengthRemoveApplyTwoHanded;
        eventService.SubscribeAll<OnPolymorphApply, OnPolymorphApply.Factory>(OnPolymorphApplyTwoHanded, EventCallbackType.After);
        eventService.SubscribeAll<OnPolymorphRemove, OnPolymorphRemove.Factory>(OnPolymorphRemoveTwoHanded, EventCallbackType.After);
        Log.Info(message: "TwoHandedBonusHandler initialized.");
    }
    
    private static void OnEquipApplyTwoHanded(OnItemEquip eventData)
    {
        if (!eventData.EquippedBy.IsPlayerControlled) return;
        if (eventData.Slot is not (InventorySlot.RightHand or InventorySlot.LeftHand)) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.EquippedBy);
    }
    
    private static void OnUnequipApplyTwoHanded(OnItemUnequip eventData)
    {
        if (!eventData.Creature.IsPlayerControlled) return;
        
        // These are hex-coded EquipableSlots from baseitems.2da to match EquipmentSlots for basic weapons
        const EquipmentSlots leftOrRightHandItem = EquipmentSlots.RightHand | EquipmentSlots.LeftHand;
        const EquipmentSlots creatureWeapon = EquipmentSlots.CreatureWeaponBite | EquipmentSlots.CreatureWeaponLeft 
            | EquipmentSlots.CreatureWeaponRight;
        const EquipmentSlots oneHandedWeapon = leftOrRightHandItem | creatureWeapon;
        const EquipmentSlots twoHandedWeapon = EquipmentSlots.RightHand | creatureWeapon;

        if (eventData.Item.BaseItem.EquipmentSlots is not (EquipmentSlots.RightHand or EquipmentSlots.LeftHand or
            leftOrRightHandItem or oneHandedWeapon or twoHandedWeapon)) return;

        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.Creature);
    }

    private static void OnStrengthApplyApplyTwoHanded(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Strength) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(creature);
    }

    private static void OnStrengthRemoveApplyTwoHanded(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Strength) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(creature);
    }

    private static void OnPolymorphApplyTwoHanded(OnPolymorphApply eventData)
    {
        if (!eventData.Creature.IsPlayerControlled) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.Creature);
    }
    
    private static void OnPolymorphRemoveTwoHanded(OnPolymorphRemove eventData)
    {
        if (!eventData.Creature.IsPlayerControlled) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.Creature);
    }
    
    
    
}