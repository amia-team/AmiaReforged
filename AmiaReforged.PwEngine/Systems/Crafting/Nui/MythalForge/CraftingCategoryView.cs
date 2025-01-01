using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class CraftingCategoryView : IView
{
    private readonly CraftingCategory _category;
    
    public CraftingCategoryView(CraftingCategory category)
    {
        _category = category;
    }

    public NuiElement GetView()
    {
        return _category.ToColumnWithGroup();
    }
}

public interface IView
{
    public NuiElement GetView();
}