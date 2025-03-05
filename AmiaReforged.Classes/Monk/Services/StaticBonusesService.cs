// An event service that applies and removes permanent static bonuses that monk, like Ki Strike, Monk Speed, Wisdom AC.
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(StaticBonusesService))]
public class StaticBonusesService
{
    private const int StaticBonusLevel = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StaticBonusesService(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");

        if (environment == "live") return;
        
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoadApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpCheckBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDownCheckBonuses, EventCallbackType.After);
        Log.Info("Monk Static Bonuses Service initialized.");
    }

    private static void OnLoadApplyBonuses(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        if (monk.ActiveEffects.Any(effect => effect.Tag == "monk_staticbonuses")) return;
        
        Effect monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
    
    private static void OnEquipApplyBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        // Only check for possible disqualifiers of monk bonuses or items with possible Wis bonus
        if (eventData.Slot is not (InventorySlot.Chest or InventorySlot.RightHand or InventorySlot.LeftHand)
            || !eventData.Item.HasItemProperty(ItemPropertyType.AbilityBonus)) return;
        
        NwCreature monk = eventData.EquippedBy;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
    
    private static void OnUnequipApplyBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        // Only check for possible disqualifiers of monk bonuses or items with possible Wis bonus
        if (eventData.Item.BaseItem.EquipmentSlots is not (EquipmentSlots.Chest or EquipmentSlots.RightHand 
                or EquipmentSlots.LeftHand) ||  !eventData.Item.HasItemProperty(ItemPropertyType.AbilityBonus)) return;
        
        // NB! the focus base item is categorized as torches in baseitems.2da
        if (eventData.Item.BaseItem.Category is not 
            (BaseItemCategory.Armor or BaseItemCategory.Shield or BaseItemCategory.Torches)) return;
        
        NwCreature monk = eventData.Creature;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
            
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level  < StaticBonusLevel) return;

        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
}
