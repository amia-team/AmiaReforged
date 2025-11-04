using AmiaReforged.Classes.Monk.Types;
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
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;

        StaticBuff.AdjustBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnEquipAdjustBuff(OnItemEquip eventData)
    {
        NwCreature monk = eventData.EquippedBy;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;

        StaticBuff.AdjustBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnUnequipAdjustBuff(OnItemUnequip eventData)
    {
        NwCreature monk = eventData.Creature;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;

        StaticBuff.AdjustBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnLevelUpAdjustBuff(OnLevelUp eventData)
    {
        NwCreature monk = eventData.Creature;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;

        StaticBuff.AdjustBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnLevelDownAdjustBuff(OnLevelDown eventData)
    {
        NwCreature monk = eventData.Creature;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;

        StaticBuff.AdjustBuff(monk);

        if (MonkUtils.GetMonkPath(monk) != PathType.FloatingLeaf) return;

        WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
    }

    private void OnWisdomApplyAdjustBuff(OnEffectApply eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        int ability = eventData.Effect.IntParams[0];
        if (ability is (int)Ability.Wisdom)
        {
            StaticBuff.AdjustBuff(monk);
        }
        if (MonkUtils.GetMonkPath(monk) == PathType.FloatingLeaf
            && ability is (int)Ability.Wisdom or (int)Ability.Dexterity or (int)Ability.Strength)
        {
            WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
        }
    }

    private void OnWisdomRemoveAdjustBuff(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature monk) return;
        if (!monk.IsMonkLevel(MinStaticBuffLevel)) return;
        if (eventData.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        int ability = eventData.Effect.IntParams[0];
        if (ability is (int)Ability.Wisdom)
        {
            StaticBuff.AdjustBuff(monk);
        }
        if (MonkUtils.GetMonkPath(monk) == PathType.FloatingLeaf
            && ability is (int)Ability.Wisdom or (int)Ability.Dexterity or (int)Ability.Strength)
        {
            WisdomAttackBonus.AdjustWisdomAttackBonus(monk);
        }
    }
}
