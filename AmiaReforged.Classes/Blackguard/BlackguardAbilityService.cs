using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
              using NLog;

namespace AmiaReforged.Classes.Blackguard;

/// <summary>
/// Governs modifications to abilities like Dark Blessing or Smite Good
/// </summary>
[ServiceBinding(typeof(BlackguardAbilityService))]
public class BlackguardAbilityService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public string BlackguardSaveOffsetTag = "BLACKGUARD_SAVE_OFFSET";

    public BlackguardAbilityService(EventService eventService)
    {
        eventService.SubscribeAll<OnLevelUp, OnLevelUp.Factory>(AdjustOnLevelUp, EventCallbackType.After);
        eventService.SubscribeAll<OnItemEquip, OnItemEquip.Factory>(AdjustOnItemEquip, EventCallbackType.After);
        eventService.SubscribeAll<OnItemUnequip, OnItemUnequip.Factory>(AdjustOnItemUnequip, EventCallbackType.After);
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(AdjustOnCharacterLoad, EventCallbackType.After);
        NwModule.Instance.OnLevelDown += AdjustOnLevelDown;
        NwModule.Instance.OnEffectRemove += AdjustOnEffectRemove;
        NwModule.Instance.OnEffectApply += AdjustOnEffectApply;

        Log.Info("Blackguard Ability Service initialized.");
    }

    private void AdjustOnCharacterLoad(OnLoadCharacterFinish obj)
    {
        if (obj.Player.ControlledCreature is not { } creature) return;
        if (!IsBlackguard(creature)) return;

        OffsetCharismaNegatives(creature);
    }

    private void AdjustOnEffectApply(OnEffectApply obj)
    {
        if (obj.Object is not NwCreature creature) return;
        if (!creature.IsLoginPlayerCharacter) return;
        if (!IsBlackguard(creature)) return;
        if (obj.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (obj.Effect.IntParams[0] != (int)Ability.Charisma) return;

        OffsetCharismaNegatives(creature);
    }

    private void AdjustOnEffectRemove(OnEffectRemove obj)
    {
        if (obj.Object is not NwCreature creature) return;
        if (!creature.IsLoginPlayerCharacter) return;
        if (!IsBlackguard(creature)) return;
        if (obj.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;
        if (obj.Effect.IntParams[0] != (int)Ability.Charisma) return;

        OffsetCharismaNegatives(creature);
    }

    private void AdjustOnItemUnequip(OnItemUnequip obj)
    {
        if (!IsBlackguard(obj.Creature)) return;
        OffsetCharismaNegatives(obj.Creature);
    }

    private void AdjustOnItemEquip(OnItemEquip obj)
    {
        if (!IsBlackguard(obj.EquippedBy)) return;
        OffsetCharismaNegatives(obj.EquippedBy);
    }

    private void AdjustOnLevelUp(OnLevelUp obj)
    {
        if (!IsBlackguard(obj.Creature)) return;
        OffsetCharismaNegatives(obj.Creature);
    }

    private void AdjustOnLevelDown(OnLevelDown obj)
    {
        if (!IsBlackguard(obj.Creature)) return;
        OffsetCharismaNegatives(obj.Creature);
    }

    private static bool IsBlackguard(NwCreature creature)
    {
        return creature.Classes.Any(c => c.Class.ClassType == ClassType.Blackguard);
    }

    private void OffsetCharismaNegatives(NwCreature creature)
    {

        Effect? existingOffset = creature.ActiveEffects.FirstOrDefault(e => e.Tag == BlackguardSaveOffsetTag);

        if (existingOffset is not null)
        {
            creature.RemoveEffect(existingOffset);
        }

        int charismaModifier = creature.GetAbilityModifier(Ability.Charisma);

        // We're just offsetting the negative charisma modifier here to prevent dark blessing from nerfing non-charisma builds.
        int bonusToOffset = charismaModifier < 0 ? Math.Abs(charismaModifier) : 0;

        Effect offsetUniversalSaves = Effect.SavingThrowIncrease(SavingThrow.All, bonusToOffset);
        offsetUniversalSaves.Tag = BlackguardSaveOffsetTag;
        offsetUniversalSaves.SubType = EffectSubType.Supernatural;

        creature.ApplyEffect(EffectDuration.Permanent, offsetUniversalSaves);
    }
}
