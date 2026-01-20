using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Blackguard;

/// <summary>
/// Governs modifications to abilities like Dark Blessing or Smite Good
/// </summary>
// [ServiceBinding(typeof(BlackguardAbilityService))]
public class BlackguardAbilityService
{
    public string BlackguardSaveOffsetTag = "BLACKGUARD_SAVE_OFFSET";

    public BlackguardAbilityService()
    {
        NwModule.Instance.OnLevelUp += AdjustOnLevelUp;
        NwModule.Instance.OnLevelDown += AdjustOnLevelDown;
        NwModule.Instance.OnItemEquip += AdjustOnItemEquip;
        NwModule.Instance.OnItemUnequip += AdjustOnItemUnequip;
        NwModule.Instance.OnEffectRemove += AdjustOnEffectRemove;
        NwModule.Instance.OnEffectApply += AdjustOnEffectApply;
    }

    private void AdjustOnEffectApply(OnEffectApply obj)
    {
        if (!obj.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.LoginCreature != obj.Object) return;
        if (player.LoginCreature is null) return;

        OffsetCharismaNegatives(player.LoginCreature);
    }

    private void AdjustOnEffectRemove(OnEffectRemove obj)
    {
        if (!obj.Object.IsPlayerControlled(out NwPlayer? player)) return;
        if (player.LoginCreature != obj.Object) return;
        if (player.LoginCreature is null) return;

        OffsetCharismaNegatives(player.LoginCreature);
    }

    private void AdjustOnItemUnequip(OnItemUnequip obj)
    {
        OffsetCharismaNegatives(obj.Creature);
    }

    private void AdjustOnItemEquip(OnItemEquip obj)
    {
        OffsetCharismaNegatives(obj.EquippedBy);
    }

    private void AdjustOnLevelUp(OnLevelUp obj)
    {
        OffsetCharismaNegatives(obj.Creature);
    }

    private void AdjustOnLevelDown(OnLevelDown obj)
    {
        OffsetCharismaNegatives(obj.Creature);
    }

    private void OffsetCharismaNegatives(NwCreature creature)
    {
        if (creature.Classes.All(c => c.Class.ClassType != ClassType.Blackguard)) return;

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
