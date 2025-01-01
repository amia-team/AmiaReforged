using AmiaReforged.Core.UserInterface;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public string NuiSelectionId { get; set; }
    public string ComboId => NuiSelectionId + "_combo";
    public CraftingCategory(string nuiSelectionId)
    {
        NuiSelectionId = nuiSelectionId;
        ComboSelection = new NuiBind<int>(nuiSelectionId);
        ShowGroup = new NuiBind<bool>(nuiSelectionId + "_show");
    }

    public NuiBind<bool> ShowGroup { get; set; }

    public NuiBind<int> ComboSelection { get; set; }
    public required string Label { get; set; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }
    

    public NuiCombo ToCombo()
    {
        List<NuiComboEntry> entries = Properties.Select((t, i) => t.ToComboEntry(i)).ToList();
        return new NuiCombo
        {
            Id = NuiSelectionId + "_combo",
            Entries = entries,
            Selected = ComboSelection
        };
    }

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
                    Id = NuiSelectionId + "_btn",
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
        if (obj.EventType == NuiEventType.Click && obj.ElementId == _categoryButton.Id)
        {
            bool show = obj.Token.GetBindValue(ShowGroup);
            
            obj.Token.SetBindValue(ShowGroup, !show);
        }
    }


    public CraftingProperty CurrentProperty(NuiWindowToken token)
    {
        return Properties[GetComboSelection(token)];
    }

    public void UpdateComboSelection(NuiWindowToken token, int value)
    {
        token.SetBindValue(ComboSelection, value);
    }

    public int GetComboSelection(NuiWindowToken token)
    {
        return token.GetBindValue(ComboSelection);
    }
}