using Anvil.API;

namespace AmiaReforged.Classes.GeneralFeats;

public static class TwoHandedBonus
{
    private const string TwoHandedBonusTag = "twohandedbonus";
    private static Effect TwoHandedBonusEffect(NwCreature creature)
    {
        // First calculate the base game 50% strength modifier bonus to twohanding
        // This is important because ints are rounded down, so you'd lose bonus damage
        // eg str mod 15 would be 7 base bonus + 7 extra bonus, while x2 str mod should add up to 15
        int strengthModifier = creature.GetAbilityModifier(Ability.Strength);
        int twoHandedDamageBonus = strengthModifier / 2;

        // Infer Amia-specific extra damage based on the base bonus
        int amiaBonus = strengthModifier - twoHandedDamageBonus;
        // Apparently values 6 to 15 in bonus dmg are weird; the incremental scaling cuts off at value 5 and continues
        // from value 16, so if our twohand damage bonus >= 6, we add 10 to continue the incremental damage scaling
        if (amiaBonus >= 6)
            amiaBonus += 10;

        Effect twoHandedDamageEffect = Effect.DamageIncrease(amiaBonus, DamageType.BaseWeapon);
        Effect twoHandedAbEffect = Effect.AttackIncrease(2);

        Effect twoHandedBonusEffect = Effect.LinkEffects(twoHandedDamageEffect, twoHandedAbEffect);
        twoHandedBonusEffect.SubType = EffectSubType.Unyielding;
        twoHandedBonusEffect.Tag = TwoHandedBonusTag;

        return twoHandedBonusEffect;
    }

    public static void ApplyTwoHandedBonusEffect(NwCreature creature)
    {
        if (creature.ControllingPlayer is not { } player) return;

        Effect? twoHandedBonus = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == TwoHandedBonusTag);

        bool hasExistingBonus = twoHandedBonus != null;

        if (twoHandedBonus != null)
            creature.RemoveEffect(twoHandedBonus);

        NwItem? weapon = creature.GetItemInSlot(InventorySlot.RightHand);

        if (weapon == null ||
            weapon.BaseItem.WeaponSize <= (BaseItemWeaponSize)creature.Size ||
            weapon.IsRangedWeapon ||
            weapon.BaseItem.WeaponWieldType == BaseItemWeaponWieldType.DoubleSided ||
            creature.GetItemInSlot(InventorySlot.LeftHand) != null)
        {
            if (hasExistingBonus)
            {
                player.SendServerMessage("Two-handed weapon bonus removed.");
            }

            return;
        }

        Effect twoHandedBonusEffect = TwoHandedBonusEffect(creature);
        creature.ApplyEffect(EffectDuration.Permanent, twoHandedBonusEffect);

        // If the bonus was already there, we know it was adjusted by str buffs/debuffs or buffs/debuffs wearing off
        string feedback = hasExistingBonus ? "Two-handed weapon bonus adjusted." : "Two-handed weapon bonus applied.";

        player.SendServerMessage(feedback);
    }
}
