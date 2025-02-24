// An event service that applies and removes permanent static bonuses that monk, like Ki Strike, Monk Speed, Wisdom AC.
using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Effects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(StaticBonusesService))]
public class StaticBonusesService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StaticBonusesService(EventService eventService)
    {
    // Register method to listen for the OnSpellCast event.
    NwModule.Instance.OnLoadCharacterFinish += OnLoadAddBonuses;
    NwModule.Instance.OnItemEquip += OnEquipRemoveBonuses;
    NwModule.Instance.OnItemUnequip += OnUnequipAddBonuses;
    eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpCheckBonuses, EventCallbackType.After);
    eventService.SubscribeAll<OnLevelDown, OnLevelDown.Factory>(OnLevelDownCheckBonuses, EventCallbackType.After);
    Log.Info("Monk Static Bonuses Service initialized.");
    }

    private static void OnLoadAddBonuses(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)!.Level >= 3) return;
        if (monk.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;

        Effect monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }

    private static void OnEquipRemoveBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)!.Level >= 3) return;

        NwCreature monk = eventData.EquippedBy;
        Effect? monkEffects = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == "monk_staticeffects");
        if (monkEffects is null) return;

        bool isShield = eventData.Item.BaseItem.Category is BaseItemCategory.Shield;
        bool isArmor = eventData.Item.BaseACValue > 0;

        if (isShield || isArmor)
        {
            monk.RemoveEffect(monkEffects);

            if (monk.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage("Monk static bonuses removed.", MonkColors.MonkColorScheme);
        }
    }

    private static void OnUnequipAddBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level >= 3) return;
        if (eventData.Creature.ActiveEffects.Any(effect => effect.Tag == "monk_staticeffects")) return;

        NwCreature monk = eventData.Creature;
        NwItem? leftHandItem = monk.GetItemInSlot(InventorySlot.LeftHand);
        NwItem? armorItem = monk.GetItemInSlot(InventorySlot.Chest);
        
        bool isNotArmor = eventData.Item.BaseACValue == 0;
        bool hasNoArmor = armorItem?.BaseACValue == 0;
        bool isNotShield = eventData.Item.BaseItem.Category is not BaseItemCategory.Shield;
        bool hasNoShield = leftHandItem?.BaseItem.Category is not BaseItemCategory.Shield;
        
        if (isNotArmor && hasNoShield || isNotShield && hasNoArmor)
        {
            Effect monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
            monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
            
            if (monk.IsPlayerControlled(out NwPlayer? player))
                player.SendServerMessage("Monk static bonuses applied.", MonkColors.MonkColorScheme);
        }
    }

    private static void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level  >= 3) return;

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
        
        if (eventData.Creature.GetClassInfo(ClassType.Monk)!.Level < 3) return;
        
        monkEffects = StaticBonusesEffect.GetStaticBonusesEffect(monk);
        monk.ApplyEffect(EffectDuration.Permanent, monkEffects);
    }
}
