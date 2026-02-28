using Anvil.API;

namespace AmiaReforged.Classes.TwoWeaponFighter;
public static class TwoWeaponDefense
{
    private static string TwoWeaponDefenseTag => "tw_defense";
    public static void ApplyTwoWeaponDefense(NwCreature creature, byte twfLevel)
    {
        Effect? existingTwoWeaponDefense =
            creature.ActiveEffects.FirstOrDefault(e => e.Tag != null && e.Tag == TwoWeaponDefenseTag);

        bool isDualWielding =
            creature.GetItemInSlot(InventorySlot.RightHand)?.BaseItem.WeaponWieldType == BaseItemWeaponWieldType.DoubleSided ||
            creature.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Melee;

        NwPlayer? player = creature.ControllingPlayer;

        int defenseAc = twfLevel switch
        {
            >= 1 and < 3 => 1,
            >= 3 and < 5 => 2,
            >= 5 => 4,
            _ => 1
        };

        if (!isDualWielding)
        {
            if (existingTwoWeaponDefense == null) return;

            creature.RemoveEffect(existingTwoWeaponDefense);
            player?.SendServerMessage($"Removed Two-Weapon Defense +{defenseAc} shield AC bonus");

            return;
        }

        Effect twoWeaponDefense = Effect.ACIncrease(defenseAc, ACBonus.ShieldEnchantment);
        twoWeaponDefense.Tag = TwoWeaponDefenseTag;
        twoWeaponDefense.SubType = EffectSubType.Unyielding;

        creature.ApplyEffect(EffectDuration.Permanent, twoWeaponDefense);

        player?.SendServerMessage($"Applied Two-Weapon Defense +{defenseAc} shield AC bonus.");
    }
}
