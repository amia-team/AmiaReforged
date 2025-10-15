using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WisdomAttackBonus
{
    private const string WisdomAbTag = "monk_wis_ab";
    public static void AdjustWisdomAttackBonus(NwCreature monk, bool abilitiesRestricted)
    {
        UnsetWisdomAttackBonus(monk);

        if (monk.IsRangedWeaponEquipped || abilitiesRestricted)
            return;

        int wisModifier = monk.GetAbilityModifier(Ability.Wisdom);
        int strModifier =  monk.GetAbilityModifier(Ability.Strength);
        int dexModifier =  monk.GetAbilityModifier(Ability.Dexterity);

        if (wisModifier <= strModifier || FinesseApplies(monk, wisModifier, dexModifier))
            return;

        // Leaving this commented out for later-day function hooking
        /*int meleeAttackBonus = monk.GetAttackBonus(isMelee: true);

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

        int newBaseAttackBonus = monk.BaseAttackBonus + wisAttackBonus - meleeAttackBonus;*/

        int wisAttackBonus =
            FinesseApplies(monk, strModifier, dexModifier) ? wisModifier - dexModifier : wisModifier - strModifier;

        Effect wisAbEffect = Effect.AttackIncrease(wisAttackBonus);
        wisAbEffect.SubType = EffectSubType.Unyielding;
        wisAbEffect.Tag = WisdomAbTag;

        monk.ApplyEffect(EffectDuration.Permanent, wisAbEffect);
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
    private static void UnsetWisdomAttackBonus(NwCreature monk)
    {
        Effect? existingAbEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == WisdomAbTag);

        if (existingAbEffect == null) return;

        monk.RemoveEffect(existingAbEffect);
    }


}


