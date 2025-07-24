// An event service that applies and removes permanent static bonuses that monk, like Ki Strike, Monk Speed, Wisdom AC.

using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(StaticBuffService))]
public class StaticBuffService
{
    private const int MinStaticBuffLevel = 3;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StaticBuffService(EventService eventService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoadApplyBonuses,
            EventCallbackType.After);
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipApplyBonuses, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpCheckBonuses, EventCallbackType.After);
        NwModule.Instance.OnLevelDown += OnLevelDownCheckBonuses;
        NwModule.Instance.OnEffectApply += OnWisdomApplyCheckBonuses;
        NwModule.Instance.OnEffectRemove += OnWisdomRemoveCheckBonuses;
        Log.Info(message: "Monk Static Bonuses Service initialized.");
    }

    private void OnLoadApplyBonuses(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(monk);
    }

    private void OnEquipApplyBonuses(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.EquippedBy);
    }

    private void OnUnequipApplyBonuses(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnLevelUpCheckBonuses(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnLevelDownCheckBonuses(OnLevelDown eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnWisdomApplyCheckBonuses(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        StaticBuff.AdjustBuff(monk);
    }

    private void OnWisdomRemoveCheckBonuses(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        StaticBuff.AdjustBuff(monk);
    }
}
