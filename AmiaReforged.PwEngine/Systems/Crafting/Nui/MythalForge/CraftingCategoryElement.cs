using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class CraftingCategoryElement : INuiModel
{
    private readonly NuiElement _element;
    public CraftingCategory Category { get; init; }
    
    private NuiButton _categoryButton;
    public NuiBind<bool> ShowGroup { get; set; }
    public NuiBind<int> Height { get; set; }
    public NuiBind<int> Width { get; set; }

    public CraftingCategoryElement(CraftingCategory category)
    {
        Category = category;
        
        List<NuiElement> properties = category.Properties.Select(t => t.ToNuiElement()).ToList();
        ShowGroup = new NuiBind<bool>(category.CategoryId + "_show");

        NuiColumn column = new()
        {
            Children =
            {
                new NuiGroup
                {
                  Element  = new NuiLabel(category.Label),
                  Width = 300f,
                  Height = 30f,
                  Border = true
                },
                new NuiGroup
                {
                    Element = new NuiColumn
                    {
                        Children = properties
                    },
                    Border = true,
                    Height = 200f,
                    Width = 300f
                }
            }
        };
        
        
        _element = column;
    }
    
    public NuiElement GetElement()
    {
        return _element;
    }
}