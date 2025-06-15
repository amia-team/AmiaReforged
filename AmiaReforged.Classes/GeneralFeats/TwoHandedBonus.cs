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
        // A slight delay to give time for things to happen
        await NwTask.Delay(TimeSpan.FromSeconds(0.1));
        
        // Safe to suppress: the caller of this code returns before executing if the creature isn't player controlled
        NwPlayer player = creature.ControllingPlayer!;
        
        // Before applying, always remove existing two-handed bonus first
        Effect? twoHandedBonus = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == "twohandedbonus");
        bool hasTwoHandedBonus = twoHandedBonus != null;
        
        if (hasTwoHandedBonus)
            creature.RemoveEffect(twoHandedBonus!);
        
        NwItem? rightHandItem = creature.GetItemInSlot(InventorySlot.RightHand);

        if (rightHandItem == null)
        {
            if (hasTwoHandedBonus)
                player.SendServerMessage("Two-handed bonus removed");
            
            return;
        }
        
        // We know that in order for the weapon to be twohanded,
        // the weapon size must always be (one size) larger than the creature
        int weaponSize = (int)rightHandItem.BaseItem.WeaponSize;
        int creatureSize = (int)creature.Size;
        
        if (weaponSize <= creatureSize)
        {
            if (hasTwoHandedBonus)
                player.SendServerMessage("Two-handed bonus removed");

            return;
        }
        
        Effect twoHandedBonusEffect = TwoHandedBonusEffect(creature);
        creature.ApplyEffect(EffectDuration.Permanent, twoHandedBonusEffect);
        
        // If the two-handed bonus was already there, it means that it was adjusted by levelling or str buffs/debuffs
        if (hasTwoHandedBonus)
            player.SendServerMessage("Two-handed bonus adjusted");
        
        // Otherwise we know that the bonus wasn't there and has been applied for the first time
        player.SendServerMessage("Two-handed bonus applied");
    }
    
}