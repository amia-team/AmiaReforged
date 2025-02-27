// An event service that applies and removes permanent static bonuses that monk, like Ki Strike, Monk Speed, Wisdom AC.
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(StaticBonusesService))]
public class StaticBonusesService
{
    private const int StaticBonusLevel = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StaticBonusesService(EventService eventService)
    {
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoadAddBonuses, EventCallbackType.After);
        NwModule.Instance.OnItemEquip += OnEquipAddBonuses;
        NwModule.Instance.OnItemEquip += OnEquipRemoveBonuses;
        NwModule.Instance.OnItemUnequip += OnUnequipAddBonuses;
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpCheckBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDownCheckBonuses, EventCallbackType.After);
        Log.Info("Monk Static Bonuses Service initialized.");
    }

    private static void OnLoadAddBonuses(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        if (monk.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;
        
        NwItem? leftHandItem = monk.GetItemInSlot(InventorySlot.LeftHand);
        NwItem? armorItem = monk.GetItemInSlot(InventorySlot.Chest);
        
        bool hasArmor = armorItem?.BaseACValue > 0;
        bool hasShield = leftHandItem?.BaseItem.Category is BaseItemCategory.Shield;

        // Don't apply effects if the monk has armor or shield
        if (hasArmor || hasShield) return;

        Effect monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnEquipRemoveBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;

        NwCreature monk = eventData.EquippedBy;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is null) return;

        bool isShield = eventData.Item.BaseItem.Category is BaseItemCategory.Shield;
        bool isArmor = eventData.Item.BaseACValue > 0;

        if (isShield || isArmor)
        {
            monk.RemoveEffect(monkEffects);
        }
        
        // Only send the server message for shield, because the game by default does it for armor
        if (!isShield) return;
        
        if (monk.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("Equipping this shield has disabled your monk abilities.");
    }
    
    /// <summary>
    /// This is necessary because the game doesn't register replacing an equipped item as unequipping;
    /// eg, if you have a shield in offhand and switch it into a kama, the game just reads it as an OnEquip event
    /// and doesn't re-add the monk bonuses although you no longer have a shield to disqualify it
    /// </summary>
    private static void OnEquipAddBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        if (eventData.EquippedBy.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;

        NwCreature monk = eventData.EquippedBy;
        NwItem? leftHandItem = monk.GetItemInSlot(InventorySlot.LeftHand);
        NwItem? armorItem = monk.GetItemInSlot(InventorySlot.Chest);
        
        bool isClothArmor = eventData.Item.BaseACValue == 0;
        bool isShield = eventData.Item.BaseItem.Category is BaseItemCategory.Shield;
        bool hasNoArmor = armorItem?.BaseACValue == 0 || armorItem is null;
        bool hasNoShield = leftHandItem?.BaseItem.Category is not BaseItemCategory.Shield || leftHandItem is null;
        
        if ((isClothArmor && hasNoShield) || (!isShield && hasNoArmor))
        {
            Effect monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
            monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        }

        if (!monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (isClothArmor && hasNoShield) 
            player.SendServerMessage ("Unequipping this armor has enabled your monk abilities.");
                
        if (!isShield && hasNoArmor) 
            player.SendServerMessage("Unequipping this shield has enabled your monk abilities.");
    }
    
    private static void OnUnequipAddBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        if (eventData.Creature.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;

        NwCreature monk = eventData.Creature;
        NwItem? leftHandItem = monk.GetItemInSlot(InventorySlot.LeftHand);
        NwItem? armorItem = monk.GetItemInSlot(InventorySlot.Chest);
        
        bool isArmor = eventData.Item.BaseACValue > 0;
        bool isShield = eventData.Item.BaseItem.Category is BaseItemCategory.Shield;
        bool hasNoArmor = armorItem?.BaseACValue == 0 || armorItem is null;
        bool hasNoShield = leftHandItem?.BaseItem.Category is not BaseItemCategory.Shield || leftHandItem is null;
        
        // If unequipping armor while isn't wielding a shield, apply bonuses
        // If unequipping shield while isn't wearing armor, apply bonuses
        if (isArmor && hasNoShield || isShield && hasNoArmor)
        {
            Effect monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
            monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        }

        if (!monk.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (isArmor && hasNoShield) 
            player.SendServerMessage ("Unequipping this armor has enabled your monk abilities.");
                
        if (isShield && hasNoArmor) 
            player.SendServerMessage("Unequipping this shield has enabled your monk abilities.");
    }

    private static void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level  < StaticBonusLevel) return;

        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is null) return;

        monk.RemoveEffect(monkEffects);
        monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is null) return;

        monk.RemoveEffect(monkEffects);
        
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
}
