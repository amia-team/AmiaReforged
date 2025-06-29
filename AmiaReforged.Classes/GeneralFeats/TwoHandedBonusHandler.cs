using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(TwoHandedBonusHandler))]
public class TwoHandedBonusHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public TwoHandedBonusHandler(EventService eventService)
    {
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyTwoHanded, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyTwoHanded, EventCallbackType.After);
        NwModule.Instance.OnEffectApply += OnStrengthApplyApplyTwoHanded;
        NwModule.Instance.OnEffectRemove += OnStrengthRemoveApplyTwoHanded;
        eventService.SubscribeAll<OnPolymorphApply, OnPolymorphApply.Factory>(OnPolymorphApplyTwoHanded, EventCallbackType.After);
        eventService.SubscribeAll<OnPolymorphRemove, OnPolymorphRemove.Factory>(OnPolymorphRemoveTwoHanded, EventCallbackType.After);
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnCharacterLoadApplyTwoHanded, EventCallbackType.After);
        Log.Info(message: "TwoHandedBonusHandler initialized.");
    }
    
    private static void OnEquipApplyTwoHanded(OnItemEquip eventData)
    {
        if (!eventData.EquippedBy.IsPlayerControlled) return;
        
        bool isWieldable = eventData.Slot is InventorySlot.RightHand or InventorySlot.LeftHand;
        
        // If the item is wieldable, we want to check for two-handed bonus and can return code early
        if (isWieldable)
        {
            TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.EquippedBy);
            return;
        }
        
        // If the item property doesn't affect abilities, we know to return early
        ItemProperty? abilityProperty = eventData.Item.ItemProperties.FirstOrDefault(p => p.Property.PropertyType is
            ItemPropertyType.AbilityBonus or ItemPropertyType.DecreasedAbilityScore);

        // If the ability affected isn't strength, we know to return early; otherwise we check for two-handed bonus
        if (abilityProperty?.IntParams[0] is not (int)Ability.Strength) return;
        
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

        bool isWieldable = eventData.Item.BaseItem.EquipmentSlots is EquipmentSlots.RightHand
            or EquipmentSlots.LeftHand or leftOrRightHandItem or oneHandedWeapon or twoHandedWeapon;
        
        // If the item is wieldable, we want to check for two-handed bonus and can return code early
        if (isWieldable)
        {
            TwoHandedBonus.ApplyTwoHandedBonusEffect(eventData.Creature);
            return;
        }
        
        // If the item property doesn't affect abilities, we know to return early
        ItemProperty? abilityProperty = eventData.Item.ItemProperties.FirstOrDefault(p => p.Property.PropertyType is
            ItemPropertyType.AbilityBonus or ItemPropertyType.DecreasedAbilityScore);

        // If the ability affected isn't strength, we know to return early; otherwise we check for two-handed bonus
        if (abilityProperty?.IntParams[0] is not (int)Ability.Strength) return;
        
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

    private static void OnCharacterLoadApplyTwoHanded(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature) return;
        
        TwoHandedBonus.ApplyTwoHandedBonusEffect(creature);
    }
    
}