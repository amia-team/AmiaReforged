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
        NwModule.Instance.OnEffectApply += OnWisdomApplyCheckBonuses;
        NwModule.Instance.OnEffectRemove += OnWisdomRemoveCheckBonuses;
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
    
    private static async void OnEquipApplyBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        // Only check for possible disqualifiers of monk bonuses
        if (eventData.Slot is not (InventorySlot.Chest or InventorySlot.RightHand or InventorySlot.LeftHand)) return;
        
        NwCreature monk = eventData.EquippedBy;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
    
    private static async void OnUnequipApplyBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        // Only check for possible disqualifiers of monk bonuses
        const EquipmentSlots leftOrRight = EquipmentSlots.RightHand | EquipmentSlots.LeftHand;
        const EquipmentSlots leftOrRightOrCreature =  
            EquipmentSlots.RightHand | EquipmentSlots.LeftHand | EquipmentSlots.CreatureWeaponBite 
            | EquipmentSlots.CreatureWeaponLeft | EquipmentSlots.CreatureWeaponRight;
        if (eventData.Item.BaseItem.EquipmentSlots is not (EquipmentSlots.Chest or EquipmentSlots.RightHand 
                or EquipmentSlots.LeftHand or leftOrRight or leftOrRightOrCreature)) return;
        
        NwCreature monk = eventData.Creature;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
            
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static async void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level  < StaticBonusLevel) return;

        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static async void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        if (monk.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;
        
        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        
        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
    
    private static void OnWisdomApplyCheckBonuses(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;

        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        ApplyStaticBonuses();
        
        return;
        
        async void ApplyStaticBonuses()
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(1));
            monkEffects = StaticBonuses.GetEffect(monk);
            monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        }
    }
    
    private static void OnWisdomRemoveCheckBonuses(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)!.Level < StaticBonusLevel) return;

        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;
        
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");
        
        if (monkEffects is not null) monk.RemoveEffect(monkEffects);
        
        ApplyStaticBonuses();
        
        return;
        
        async void ApplyStaticBonuses()
        {
            await NwTask.Delay(TimeSpan.FromMilliseconds(1));
            monkEffects = StaticBonuses.GetEffect(monk);
            monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        }
    }
}
