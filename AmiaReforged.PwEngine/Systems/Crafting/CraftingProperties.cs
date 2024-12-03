using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

/// <summary>
/// I felt it was better to use fixed properties for crafting, rather than a dynamic system, because it can get
/// difficult to manage and balance.
/// </summary>
public static class CraftingProperties
{
    
    // Enhancement bonuses
    public static EnhancementBonuses EnhancementBonuses => new EnhancementBonuses();

    // Armor class bonuses
    public static 
        ArmorClassBonuses ArmorClassBonuses => new ArmorClassBonuses();
}