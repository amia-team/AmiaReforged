using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(StaticBuffService))]
public class StaticBuffService
{
    private static readonly NwFeat? MonkDefense = NwFeat.FromFeatId(MonkFeat.MonkDefense);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public StaticBuffService(EventService eventService)
    {
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
        if (eventData.Player.ControlledCreature is not { } monk
            || !monk.KnowsFeat(MonkDefense!)) return;

        _ = StaticBuff.RefreshBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        _ = WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnEquipAdjustBuff(OnItemEquip eventData)
    {
        if (!eventData.EquippedBy.KnowsFeat(MonkDefense!)) return;

        _ = StaticBuff.RefreshBuff(eventData.EquippedBy);

        if (MonkUtils.GetMonkPath(eventData.EquippedBy) != PathType.FloatingLeaf) return;

        _ = WisdomAttackBonus.AdjustWisdomAttackBonus(eventData.EquippedBy);
    }

    private void OnUnequipAdjustBuff(OnItemUnequip eventData)
    {
        if (!eventData.Creature.KnowsFeat(MonkDefense!)) return;

        _ = StaticBuff.RefreshBuff(eventData.Creature);

        if (MonkUtils.GetMonkPath(eventData.Creature) != PathType.FloatingLeaf) return;

        _ = WisdomAttackBonus.AdjustWisdomAttackBonus(eventData.Creature);
    }

    private void OnLevelUpAdjustBuff(OnLevelUp eventData)
    {
        if (!eventData.Creature.KnowsFeat(MonkDefense!)) return;

        _ = StaticBuff.RefreshBuff(eventData.Creature);

        if (MonkUtils.GetMonkPath(eventData.Creature) != PathType.FloatingLeaf) return;

        _ = WisdomAttackBonus.AdjustWisdomAttackBonus(eventData.Creature);
    }

    private void OnLevelDownAdjustBuff(OnLevelDown eventData)
    {
        if (!eventData.Creature.KnowsFeat(MonkDefense!)) return;

        _ = StaticBuff.RefreshBuff(eventData.Creature);

        if (MonkUtils.GetMonkPath(eventData.Creature) != PathType.FloatingLeaf) return;

        _ = WisdomAttackBonus.AdjustWisdomAttackBonus(eventData.Creature);
    }

    private void OnWisdomApplyAdjustBuff(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk
            || !monk.KnowsFeat(MonkDefense!)
            || eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease))
            return;

        int ability = eventData.Effect.IntParams[0];
        if (ability is (int)Ability.Wisdom)
        {
            _ = StaticBuff.RefreshBuff(monk);
        }
        if (MonkUtils.GetMonkPath(monk) == PathType.FloatingLeaf
            && ability is (int)Ability.Wisdom or (int)Ability.Dexterity or (int)Ability.Strength)
        {
            _ = WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
        }
    }

    private void OnWisdomRemoveAdjustBuff(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk
            ||!monk.KnowsFeat(MonkDefense!)
            || eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease))
            return;

        int ability = eventData.Effect.IntParams[0];
        if (ability is (int)Ability.Wisdom)
        {
            _ = StaticBuff.RefreshBuff(monk);
        }
        if (MonkUtils.GetMonkPath(monk) == PathType.FloatingLeaf
            && ability is (int)Ability.Wisdom or (int)Ability.Dexterity or (int)Ability.Strength)
        {
            _ = WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
        }
    }
}
