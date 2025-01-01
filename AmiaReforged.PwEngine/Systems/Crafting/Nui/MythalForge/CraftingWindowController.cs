using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class CraftingWindowController
{
    private readonly CraftingCategoryController _categorySubView;
    private readonly ItemPropertyListController _craftingWindowView;
    
    
    private readonly NwPlayer _player;
    private readonly NwItem _selection;
    
    
    public CraftingWindowController(NwPlayer player, NwItem selection)
    {
        _player = player;
        _selection = selection;
        
    }
}

public class ChangelistEntry
{
    public readonly CraftingCategory Category;
    public readonly CraftingProperty Property;
    public readonly int GpCost;
}