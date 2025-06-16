using Anvil.API;

namespace AmiaReforged.Classes.GeneralFeats;

public static class TwoHandedBonus
{
    private static Effect TwoHandedBonusEffect(NwCreature creature)
    {
        // First calculate the base game 50% strength modifier bonus to twohanding
        // This is important because ints are rounded down, so you'd lose bonus damage
        // eg str mod 15 would be 7 base bonus + 7 extra bonus, while x2 str mod should add up to 15
        int strengthModifier = creature.GetAbilityModifier(Ability.Strength);
        int baseTwoHandedDamageBonus = (int)(strengthModifier * 1.5 - strengthModifier);
        
        // Infer Amia-specific extra damage based on the base bonus
        int extraTwoHandedDamageBonus = strengthModifier - baseTwoHandedDamageBonus;

        Effect twoHandedDamageEffect = Effect.DamageIncrease(extraTwoHandedDamageBonus, DamageType.BaseWeapon);

        const int twoHandedExtraAbBonus = 2;

        Effect twoHandedAbEffect = Effect.AttackIncrease(twoHandedExtraAbBonus);
        
        Effect twoHandedBonusEffect = Effect.LinkEffects(twoHandedDamageEffect, twoHandedAbEffect);
        twoHandedBonusEffect.SubType = EffectSubType.Unyielding;
        twoHandedBonusEffect.Tag = "twohandedbonus";
        
        return twoHandedBonusEffect;
    }

    public static async Task ApplyTwoHandedBonusEffect(NwCreature creature)
    {
        // Safe to suppress: the caller of this code returns before executing if the creature isn't player controlled
        NwPlayer player = creature.ControllingPlayer!;
        Effect? twoHandedBonus = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == "twohandedbonus");
        NwItem? weapon = creature.GetItemInSlot(InventorySlot.RightHand);
        
        // Check if the creature has a pre-existing two-handed bonus; always remove it before reapplying the new one
        bool hasTwoHandedBonus = twoHandedBonus != null;
        
        if (hasTwoHandedBonus)
            creature.RemoveEffect(twoHandedBonus!);
        
        // Check if the creature doesn't have a right hand item
        bool hasNoWeapon = weapon == null;
        
        // We know that in order for the weapon to be twohanded,
        // the weapon size must always be (one size) larger than the creature
        int weaponSize = (int)weapon!.BaseItem.WeaponSize;
        int creatureSize = (int)creature.Size;
        
        bool weaponIsNotTwoHanded = weaponSize <= creatureSize;
        
        // Check if the weapon is ranged (safe to suppress) 
        bool weaponIsRanged = weapon.IsRangedWeapon;
        
        // Check if the weapon is a UBAB weapon
        bool weaponIsMonkWeapon = weapon.BaseItem.IsMonkWeapon;

        // A slight delay to give time for things to happen
        await NwTask.Delay(TimeSpan.FromSeconds(0.1));
        
        // Disqualifiers for two-handed bonus
        if (hasNoWeapon || weaponIsNotTwoHanded || weaponIsRanged || weaponIsMonkWeapon)
        {
            if (hasTwoHandedBonus)
                player.SendServerMessage("Two-handed weapon bonus removed.");
            
            return;
        }
        
        // If the code made it this far, we have qualified for two-handed bonus!
        Effect twoHandedBonusEffect = TwoHandedBonusEffect(creature);
        creature.ApplyEffect(EffectDuration.Permanent, twoHandedBonusEffect);
        
        // If the bonus was already there, we know it was adjusted by str buffs/debuffs or buffs/debuffs wearing off
        if (hasTwoHandedBonus)
        {
            player.SendServerMessage("Two-handed weapon bonus adjusted.");
            return;
        }
        
        // Otherwise we know that the bonus wasn't there and has been applied for the first time
        player.SendServerMessage("Two-handed weapon bonus applied.");
    }
    
}