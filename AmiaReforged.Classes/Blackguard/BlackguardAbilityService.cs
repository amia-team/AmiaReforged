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

    /// <summary>
    /// Tracks creatures currently having their offset adjusted to prevent recursion.
    /// </summary>
    private readonly HashSet<NwCreature> _adjustingCreatures = new();

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
        try
        {
            if (obj.Player?.ControlledCreature is not { } creature) return;
            if (!IsBlackguard(creature)) return;

            OffsetCharismaNegatives(creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnCharacterLoad");
        }
    }

    private void AdjustOnEffectApply(OnEffectApply obj)
    {
        try
        {
            if (obj.Object is not NwCreature creature) return;
            if (!creature.IsLoginPlayerCharacter) return;
            if (!IsBlackguard(creature)) return;
            if (obj.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;

            // Skip our own effect to prevent recursion
            if (obj.Effect.Tag == BlackguardSaveOffsetTag) return;

            if (obj.Effect.IntParams[0] != (int)Ability.Charisma) return;

            OffsetCharismaNegatives(creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnEffectApply");
        }
    }

    private void AdjustOnEffectRemove(OnEffectRemove obj)
    {
        try
        {
            if (obj.Object is not NwCreature creature) return;
            if (!creature.IsLoginPlayerCharacter) return;
            if (!IsBlackguard(creature)) return;
            if (obj.Effect.EffectType is not (EffectType.AbilityIncrease or EffectType.AbilityDecrease)) return;

            // Skip our own effect to prevent recursion
            if (obj.Effect.Tag == BlackguardSaveOffsetTag) return;

            if (obj.Effect.IntParams[0] != (int)Ability.Charisma) return;

            OffsetCharismaNegatives(creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnEffectRemove");
        }
    }

    private void AdjustOnItemUnequip(OnItemUnequip obj)
    {
        try
        {
            if (obj.Creature == null) return;
            if (!IsBlackguard(obj.Creature)) return;
            OffsetCharismaNegatives(obj.Creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnItemUnequip");
        }
    }

    private void AdjustOnItemEquip(OnItemEquip obj)
    {
        try
        {
            if (obj.EquippedBy == null) return;
            if (!IsBlackguard(obj.EquippedBy)) return;
            OffsetCharismaNegatives(obj.EquippedBy);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnItemEquip");
        }
    }

    private void AdjustOnLevelUp(OnLevelUp obj)
    {
        try
        {
            if (obj.Creature == null) return;
            if (!IsBlackguard(obj.Creature)) return;
            OffsetCharismaNegatives(obj.Creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnLevelUp");
        }
    }

    private void AdjustOnLevelDown(OnLevelDown obj)
    {
        try
        {
            if (obj.Creature == null) return;
            if (!IsBlackguard(obj.Creature)) return;
            OffsetCharismaNegatives(obj.Creature);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in AdjustOnLevelDown");
        }
    }

    private static bool IsBlackguard(NwCreature creature)
    {
        return creature.Classes.Any(c => c.Class.ClassType == ClassType.Blackguard);
    }

    private void OffsetCharismaNegatives(NwCreature creature)
    {
        // Recursion guard - if we're already adjusting this creature, bail out
        if (!_adjustingCreatures.Add(creature)) return;

        try
        {
            // Use ToList() to avoid collection-modified exceptions during enumeration
            Effect? existingOffset = creature.ActiveEffects.ToList()
                .FirstOrDefault(e => e.Tag == BlackguardSaveOffsetTag);

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
        catch (Exception ex)
        {
            Log.Error(ex, "BlackguardAbilityService: Error in OffsetCharismaNegatives for creature {CreatureName}", creature.Name);
        }
        finally
        {
            _adjustingCreatures.Remove(creature);
        }
    }
}
