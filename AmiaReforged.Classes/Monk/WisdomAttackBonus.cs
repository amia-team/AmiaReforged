using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WisdomAttackBonus
{
    public static void AdjustWisdomAttackBonus(NwCreature monk, bool abilitiesRestricted)
    {
        if (monk.IsRangedWeaponEquipped || abilitiesRestricted)
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        int wisModifier = monk.GetAbilityModifier(Ability.Wisdom);
        int strModifier =  monk.GetAbilityModifier(Ability.Strength);
        int dexModifier =  monk.GetAbilityModifier(Ability.Dexterity);

        if (wisModifier <= strModifier || FinesseApplies(monk, wisModifier, dexModifier))
        {
            UnsetWisdomAttackBonus(monk);
            return;
        }

        int meleeAttackBonus = monk.GetAttackBonus(isMelee: true);

        if (monk.GetItemInSlot(InventorySlot.RightHand) is null
            && monk.GetItemInSlot(InventorySlot.Arms) is { } gloves
            && gloves.ItemProperties.Any(ip => ip.Property.PropertyType == ItemPropertyType.AttackBonus))
        {
            int glovesAb = gloves.ItemProperties.
                First(ip => ip.Property.PropertyType == ItemPropertyType.AttackBonus).
                IntParams[0];

            meleeAttackBonus += glovesAb;
        }

        int wisAttackBonus =
            FinesseApplies(monk, strModifier, dexModifier) ?
                meleeAttackBonus - dexModifier + wisModifier
                : meleeAttackBonus - strModifier + wisModifier;

        int newBaseAttackBonus = monk.BaseAttackBonus + wisAttackBonus - meleeAttackBonus;

        monk.BaseAttackBonus = newBaseAttackBonus;
    }

    private static bool FinesseApplies(NwCreature monk, int abilityModifier, int dexModifier)
    {
        NwItem? weapon = monk.GetItemInSlot(InventorySlot.RightHand);
        bool hasFinesseWeapon = weapon is null || monk.Size <= weapon.BaseItem.WeaponFinesseMinimumCreatureSize;

        return monk.KnowsFeat(Feat.WeaponFinesse!) && hasFinesseWeapon && abilityModifier <= dexModifier;
    }

    /// <summary>
    /// Setting BAB to 0 reverts to original BAB
    /// </summary>
    private static void UnsetWisdomAttackBonus(NwCreature monk) =>
        monk.BaseAttackBonus = 0;


}


