using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WisdomAttackBonus
{
    public static void AdjustWisdomAttackBonus(NwCreature monk, bool abilitiesRestricted)
    {
        if (monk.IsRangedWeaponEquipped)
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        if (abilitiesRestricted)
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        int wisModifier = monk.GetAbilityModifier(Ability.Wisdom);
        int strModifier =  monk.GetAbilityModifier(Ability.Strength);
        int dexModifier =  monk.GetAbilityModifier(Ability.Dexterity);

        if (wisModifier <= strModifier)
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        NwItem? weapon = monk.GetItemInSlot(InventorySlot.RightHand);

        bool isFinesseWeapon = weapon is null || weapon.BaseItem.WeaponFinesseMinimumCreatureSize <= monk.Size;

        if (monk.HasFeatEffect(Feat.WeaponFinesse!) && isFinesseWeapon && wisModifier <= dexModifier)
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        int meleeAttackBonus = monk.GetAttackBonus(true);

        if (monk.GetItemInSlot(InventorySlot.RightHand) is null
            && monk.GetItemInSlot(InventorySlot.Arms) is { } gloves
            && gloves.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.AttackBonus))
        {
            ItemProperty glovesAbProperty =
                gloves.ItemProperties.First(ip => ip.Property.PropertyType == ItemPropertyType.AttackBonus);
            int glovesAb = glovesAbProperty.IntParams[0];
            meleeAttackBonus += glovesAb;
        }

        int wisModifiedAttackBonus =
            dexModifier > strModifier ?
                meleeAttackBonus - dexModifier + wisModifier
                : meleeAttackBonus - strModifier + wisModifier;

        int newBaseAttackBonus = monk.BaseAttackBonus + wisModifiedAttackBonus - meleeAttackBonus;

        monk.BaseAttackBonus = newBaseAttackBonus;
    }

    /// <summary>
    /// Setting BAB to 0 reverts to original BAB
    /// </summary>
    private static void UnsetWisdomAttackBonus(NwCreature monk) =>
        monk.BaseAttackBonus = 0;
}


