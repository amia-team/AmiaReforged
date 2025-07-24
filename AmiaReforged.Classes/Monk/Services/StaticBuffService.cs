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

        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(OnLoadAdjustBuff,
            EventCallbackType.After);
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(OnEquipAdjustBuff, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(OnUnequipAdjustBuff, EventCallbackType.After);
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(OnLevelUpAdjustBuff, EventCallbackType.After);
        NwModule.Instance.OnLevelDown += OnLevelDownAdjustBuff;
        NwModule.Instance.OnEffectApply += OnWisdomApplyAdjustBuff;
        NwModule.Instance.OnEffectRemove += OnWisdomRemoveAdjustBuff;
        Log.Info(message: "Monk Static Bonuses Service initialized.");
    }

    private void OnLoadAdjustBuff(OnLoadCharacterFinish eventData)
    {
        if (eventData.Player.ControlledCreature is not { } monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(monk);
    }

    private void OnEquipAdjustBuff(OnItemEquip eventData)
    {
        if (eventData.EquippedBy.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.EquippedBy);
    }

    private void OnUnequipAdjustBuff(OnItemUnequip eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnLevelUpAdjustBuff(OnLevelUp eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnLevelDownAdjustBuff(OnLevelDown eventData)
    {
        if (eventData.Creature.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;

        StaticBuff.AdjustBuff(eventData.Creature);
    }

    private void OnWisdomApplyAdjustBuff(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        StaticBuff.AdjustBuff(monk);
    }

    private void OnWisdomRemoveAdjustBuff(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (monk.GetClassInfo(ClassType.Monk)?.Level is null or < MinStaticBuffLevel) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (eventData.Effect.IntParams[0] is not (int)Ability.Wisdom) return;

        StaticBuff.AdjustBuff(monk);
    }
}
