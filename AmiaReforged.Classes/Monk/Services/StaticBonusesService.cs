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
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoadApplyBonuses,
            EventCallbackType.After);
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpCheckBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDownCheckBonuses, EventCallbackType.After);
        NwModule.Instance.OnEffectApply += OnWisdomApplyCheckBonuses;
        NwModule.Instance.OnEffectRemove += OnWisdomRemoveCheckBonuses;
        Log.Info(message: "Monk Static Bonuses Service initialized.");
    }

    private static void OnLoadApplyBonuses(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;
        if (monk.ActiveEffects.Any(effect => effect.Tag == "monk_staticbonuses")) return;

        Effect monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnEquipApplyBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;

        // First check for possible disqualifying mainhands, offhands, armors
        if (eventData.Slot is not (InventorySlot.Chest or InventorySlot.RightHand or InventorySlot.LeftHand))
            // Then check for items with wisdom properties as they can increase the Wis AC
            if (!eventData.Item.ItemProperties.Any(ip => ip.IntParams[0] is not (int)Ability.Wisdom))
                return;

        NwCreature monk = eventData.EquippedBy;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        // Block wisdom attack bonus for ranged weapons
        if (monk.IsRangedWeaponEquipped)
        {
            WisdomAttackBonus.UnsetWisdomAttackBonus(monk);
            return;
        }
        
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }

    private static void OnUnequipApplyBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;

        // These are hex-coded EquipableSlots from baseitems.2da to match EquipmentSlots for basic weapons
        const EquipmentSlots leftOrRight = EquipmentSlots.RightHand | EquipmentSlots.LeftHand;
        const EquipmentSlots leftOrRightOrCreature =
            EquipmentSlots.RightHand | EquipmentSlots.LeftHand | EquipmentSlots.CreatureWeaponBite
            | EquipmentSlots.CreatureWeaponLeft | EquipmentSlots.CreatureWeaponRight;

        // First check for possible disqualifying mainhands, offhands, armors
        if (eventData.Item.BaseItem.EquipmentSlots is not (EquipmentSlots.Chest or EquipmentSlots.RightHand
            or EquipmentSlots.LeftHand or leftOrRight or leftOrRightOrCreature))
            // Then check for items with wisdom properties as they can increase the Wis AC
            if (!eventData.Item.ItemProperties.Any(ip => ip.IntParams[0] is not (int)Ability.Wisdom))
                return;

        NwCreature monk = eventData.Creature;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }

    private static void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;

        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }

    private static void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
        
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }

    private static void OnWisdomApplyCheckBonuses(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
            
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }

    private static void OnWisdomRemoveCheckBonuses(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < StaticBonusLevel) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticbonuses");

        if (monkEffects is not null) monk.RemoveEffect(monkEffects);

        monkEffects = StaticBonuses.GetEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
            
        // This checks only for the wis monk path
        WisdomAttackBonus.SetWisdomAttackBonus(monk);
    }
}