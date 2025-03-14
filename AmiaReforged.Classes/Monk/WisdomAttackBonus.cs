using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class WisdomAttackBonus
{
    public static void SetWisdomAttackBonus(NwCreature monk)
    {
        if (MonkUtilFunctions.GetMonkPath(monk) != PathType.CrystalTides) return;
        if (monk.IsRangedWeaponEquipped) return;
        
        int meleeAttackBonus = monk.GetAttackBonus(true);
        
        int wisModifier = monk.GetAbilityModifier(Ability.Wisdom);
        int strModifier =  monk.GetAbilityModifier(Ability.Strength);
        int dexModifier =  monk.GetAbilityModifier(Ability.Dexterity);
        
        NwItem? weapon = monk.GetItemInSlot(InventorySlot.RightHand);
        
        bool isFinesseWeapon = weapon is null || weapon.BaseItem.WeaponFinesseMinimumCreatureSize <= monk.Size;
        bool useDex = monk.HasFeatEffect(Feat.WeaponFinesse!) && dexModifier > strModifier && isFinesseWeapon;
        
        bool useDexAndWisHigher = useDex && meleeAttackBonus - dexModifier + wisModifier < meleeAttackBonus;
        bool useStrAndWisHigher = !useDex && meleeAttackBonus - strModifier + wisModifier < meleeAttackBonus;
        
        int wisModifiedAttackBonus = useDexAndWisHigher ? meleeAttackBonus - dexModifier + wisModifier :
            useStrAndWisHigher ? meleeAttackBonus - strModifier + wisModifier : meleeAttackBonus;
        
        if (wisModifiedAttackBonus <= meleeAttackBonus) return;

        int newBaseAttackBonus = monk.BaseAttackBonus + wisModifiedAttackBonus - meleeAttackBonus;

        monk.BaseAttackBonus = newBaseAttackBonus;
    }
    
    public static void UnsetWisdomAttackBonus(NwCreature monk)
    {
        // Setting to 0 reverts to original BAB
        monk.BaseAttackBonus = 0;
    }
}
  

