using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WisdomAttackBonus
{
    private const string WisdomAbTag = "monk_wis_ab";
    public static async Task AdjustWisdomAttackBonus(NwCreature monk)
    {
        await NwTask.Delay(TimeSpan.FromMilliseconds(10));
        UnsetWisdomAttackBonus(monk);

        if (monk.IsRangedWeaponEquipped || MonkUtils.AbilityRestricted(monk, "Floating Leaf attack bonus"))
            return;

        int wisModifier = monk.GetAbilityModifier(Ability.Wisdom);
        int strModifier =  monk.GetAbilityModifier(Ability.Strength);
        int dexModifier =  monk.GetAbilityModifier(Ability.Dexterity);

        if (wisModifier <= strModifier || monk.FinesseApplies(wisModifier, dexModifier)) return;

        int wisAttackBonus = monk.FinesseApplies(strModifier, dexModifier)
                ? wisModifier - dexModifier
                : wisModifier - strModifier;

        Effect wisAbEffect = Effect.AttackIncrease(wisAttackBonus);
        wisAbEffect.SubType = EffectSubType.Unyielding;
        wisAbEffect.Tag = WisdomAbTag;

        monk.ApplyEffect(EffectDuration.Permanent, wisAbEffect);
    }

    private static bool FinesseApplies(this NwCreature monk, int abilityModifier, int dexModifier)
    {
        NwItem? weapon = monk.GetItemInSlot(InventorySlot.RightHand);
        bool hasFinesseWeapon = weapon is null || monk.Size <= weapon.BaseItem.WeaponFinesseMinimumCreatureSize;

        return hasFinesseWeapon && abilityModifier <= dexModifier;
    }

    private static void UnsetWisdomAttackBonus(NwCreature monk)
    {
        Effect? existingAbEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == WisdomAbTag);

        if (existingAbEffect == null) return;

        monk.RemoveEffect(existingAbEffect);
    }
}


