using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Duelist;

[ServiceBinding(typeof(DuelistBonusService))]
public class DuelistBonusService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const int DuelistClassId = 52;

    public DuelistBonusService(EventService eventService)
    {
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipCheckBonus, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipCheckBonus, EventCallbackType.After);
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoginCheckBonus,
            EventCallbackType.After);

        Log.Info(message: "Duelist Bonus Service initialized.");
    }

    private void OnEquipCheckBonus(OnItemEquip eventData)
    {
        if (DuelistLevel(eventData.EquippedBy) <= 0
            || eventData.Slot is not (InventorySlot.RightHand or InventorySlot.LeftHand)) return;

        DuelistBonusEffect.ApplyDuelistBonusEffect(eventData.EquippedBy, DuelistLevel(eventData.EquippedBy));
    }

    private void OnUnequipCheckBonus(OnItemUnequip eventData)
    {
        if (DuelistLevel(eventData.Creature) <= 0) return;

        DuelistBonusEffect.ApplyDuelistBonusEffect(eventData.Creature, DuelistLevel(eventData.Creature));
    }

    private void OnLoginCheckBonus(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } creature ||
            DuelistLevel(eventData.Player.ControlledCreature) <= 0) return;

        DuelistBonusEffect.ApplyDuelistBonusEffect(creature, DuelistLevel(creature));
    }

    private static int DuelistLevel(NwCreature creature)
        => NWScript.GetLevelByClass(DuelistClassId, creature);
}
