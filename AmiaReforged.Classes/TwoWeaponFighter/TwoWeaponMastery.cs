using Anvil.API;

namespace AmiaReforged.Classes.TwoWeaponFighter;

public static class TwoWeaponMastery
{
    private static string TwoWeaponMasteryTag => "tw_mastery";
    public static void ApplyDualMastery(NwCreature creature)
    {
        NwItem? offHandItem = creature.GetItemInSlot(InventorySlot.LeftHand);

        bool hasMediumOffHand =
            offHandItem != null && offHandItem.BaseItem.Category == BaseItemCategory.Melee &&
            (int)offHandItem.BaseItem.WeaponSize == (int)creature.Size;

        Effect? existingTwoWeaponMastery =
            creature.ActiveEffects.FirstOrDefault(e => e.Tag != null && e.Tag == TwoWeaponMasteryTag);

        NwPlayer? player = creature.ControllingPlayer;

        if (!hasMediumOffHand)
        {
            if (existingTwoWeaponMastery == null) return;

            creature.RemoveEffect(existingTwoWeaponMastery);
            player?.SendServerMessage("Removed Two-Weapon Mastery +2 attack bonus.");

            return;
        }

        Effect twoWeaponMastery = Effect.AttackIncrease(2);
        twoWeaponMastery.Tag = "tw_mastery";
        twoWeaponMastery.SubType = EffectSubType.Unyielding;

        creature.ApplyEffect(EffectDuration.Permanent, twoWeaponMastery);

        player?.SendServerMessage("Applied Two Weapon Mastery +2 attack bonus.");
    }
}
