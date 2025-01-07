using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public string CategoryId { get; set; }
    public string ComboId => CategoryId + "_combo";
    public CraftingCategory(string categoryId)
    {
        CategoryId = categoryId;
        ComboSelection = new NuiBind<int>(categoryId);
        ShowGroup = new NuiBind<bool>(categoryId + "_show");
    }

    public NuiBind<bool> ShowGroup { get; set; }

    public NuiBind<int> ComboSelection { get; set; }
    public required string Label { get; set; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }
    
    private NuiButton _categoryButton;

    public NuiColumn ToColumnWithGroup()
    {
        List<NuiElement> properties = Properties.Select(t => t.ToNuiElement()).ToList();

        NuiColumn column = new()
        {
            Children =
            {
                new NuiButton(Label)
                {
                    Id = CategoryId + "_btn",
                }.Assign(out _categoryButton),
                new NuiGroup
                {
                    Element = new NuiColumn
                    {
                        Children = properties
                    },
                    Border = true,
                    Visible = ShowGroup
                }
            }
        };

        NwModule.Instance.OnNuiEvent += OnCategoryPress;
        return column;
    }

    private void OnCategoryPress(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click || obj.ElementId != _categoryButton.Id) return;
        
        bool show = obj.Token.GetBindValue(ShowGroup);
            
        obj.Token.SetBindValue(ShowGroup, !show);
    }

    public void UpdateComboSelection(NuiWindowToken token, int value)
    {
        token.SetBindValue(ComboSelection, value);
    }
}