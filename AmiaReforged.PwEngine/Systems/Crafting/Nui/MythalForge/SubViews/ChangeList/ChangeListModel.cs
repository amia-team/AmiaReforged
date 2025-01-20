using AmiaReforged.PwEngine.Systems.Crafting.Models;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;

public class ChangeListModel
{
    private readonly List<ChangelistEntry> _changeList = new();

    private readonly List<ChangelistEntry> _removedProperties = new();
    private readonly List<ChangelistEntry> _addedProperties = new();

    public void AddRemovedProperty(CraftingProperty property)
    {
        ChangelistEntry entry = new()
        {
            Label = property.GuiLabel,
            Property = property,
            GpCost = property.GoldCost,
            State = ChangeState.Removed
        };

        _removedProperties.Add(entry);
    }

    public void AddNewProperty(CraftingProperty property)
    {
        ChangelistEntry entry = new()
        {
            Label = property.GuiLabel,
            Property = property,
            GpCost = property.GoldCost,
            State = ChangeState.Added
        };

        _addedProperties.Add(entry);
    }
    
    public List<ChangelistEntry> ChangeList() =>
        _removedProperties.Concat(_addedProperties).Concat(_changeList).ToList();

    public class ChangelistEntry
    {
        public required string Label { get; set; }
        public required CraftingProperty Property { get; set; }
        public int GpCost { get; set; }
        public ChangeState State { get; set; }
    }

    public enum ChangeState
    {
        Added,
        Removed,
        Replaced
    }
}