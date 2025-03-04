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
        if (monk.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;
        
        Effect monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        OnEffectApply onEffectApply = new();
        AbilityRestrictionsHandler.PreventStaticBonuses(onEffectApply);
    }
    
    /// <summary>
    /// This is necessary because the game doesn't register replacing an equipped item as unequipping;
    /// eg, if you have a shield in offhand and switch it into a kama, the game just reads it as an OnEquip event
    /// and doesn't re-add the monk bonuses although you no longer have a shield to disqualify it
    /// </summary>
    private static void OnEquipApplyBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        if (eventData.Slot is not (InventorySlot.RightHand or InventorySlot.LeftHand or InventorySlot.Chest)) return;
        
        NwCreature monk = eventData.EquippedBy;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);

        OnEffectApply onEffectApply = new();
        AbilityRestrictionsHandler.PreventStaticBonuses(onEffectApply);
    }
    
    private static void OnUnequipApplyBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        // NB! the focus base item is categorized as torches in baseitems.2da
        if (eventData.Item.BaseItem.Category is not 
            (BaseItemCategory.Armor or BaseItemCategory.Shield or BaseItemCategory.Torches)) return;
        
        NwCreature monk = eventData.Creature;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
            
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        OnEffectApply onEffectApply = new();
        AbilityRestrictionsHandler.PreventStaticBonuses(onEffectApply);
    }

    private static void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level  < StaticBonusLevel) return;

        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is null) return;

        monk.RemoveEffect(monkEffects);
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        
        if (monkEffects is null) return;

        monk.RemoveEffect(monkEffects);
        
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
}
