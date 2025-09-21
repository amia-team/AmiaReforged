using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.TwoWeaponFighter;

[ServiceBinding(typeof(TwoWeaponHandler))]
public class TwoWeaponHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwClass? _twoWeaponFighter = NwClass.FromClassId(46);

    public TwoWeaponHandler(EventService eventService)
    {
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyDual, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyDual, EventCallbackType.After);
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnCharacterLoadApplyDual, EventCallbackType.After);

        Log.Info(message: "TwoHandedBonusHandler initialized.");
    }

    private void OnEquipApplyDual(OnItemEquip eventData)
    {
        byte twfLevel = eventData.EquippedBy.GetClassInfo(_twoWeaponFighter)?.Level ?? 0;
        if (twfLevel == 0) return;
        if (eventData.Slot is not (InventorySlot.RightHand or InventorySlot.LeftHand)) return;

        TwoWeaponDefense.ApplyTwoWeaponDefense(eventData.EquippedBy, twfLevel);

        if (twfLevel < 5) return;

        TwoWeaponMastery.ApplyDualMastery(eventData.EquippedBy);
    }

    private void OnUnequipApplyDual(OnItemUnequip eventData)
    {
        byte twfLevel = eventData.Creature.GetClassInfo(_twoWeaponFighter)?.Level ?? 0;
        if (twfLevel == 0) return;

        TwoWeaponDefense.ApplyTwoWeaponDefense(eventData.Creature, twfLevel);

        if (twfLevel < 5) return;

        TwoWeaponMastery.ApplyDualMastery(eventData.Creature);
    }

    private void OnCharacterLoadApplyDual(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature) return;
        byte twfLevel = creature.GetClassInfo(_twoWeaponFighter)?.Level ?? 0;
        if (twfLevel == 0) return;

        TwoWeaponDefense.ApplyTwoWeaponDefense(creature, twfLevel);

        if (twfLevel < 5) return;

        TwoWeaponMastery.ApplyDualMastery(creature);
    }
}
