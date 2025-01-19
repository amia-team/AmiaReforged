using AmiaReforged.PwEngine.Systems.Crafting.Models;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;

public class ChangeListModel
{
    public readonly List<ChangelistEntry> ChangeList = new();

    public void AddProperty(CraftingProperty property)
    {
        ChangelistEntry entry = new()
        {
            Label = property.GuiLabel,
            Property = property,
            GpCost = property.GoldCost,
            State = ChangeState.Added
        };

        ChangeList.Add(entry);
    }
    
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