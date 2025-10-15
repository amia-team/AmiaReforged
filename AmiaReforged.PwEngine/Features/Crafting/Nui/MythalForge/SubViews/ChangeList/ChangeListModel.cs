using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;

public class ChangeListModel
{
    public enum ChangeState
    {
        Added,
        Removed
    }

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<ChangelistEntry> _addedProperties = new();

    private readonly List<ChangelistEntry> _removedProperties = new();

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

    public void AddNewProperty(MythalCategoryModel.MythalProperty property)
    {
        ChangelistEntry entry = new()
        {
            Label = property.Internal.GuiLabel,
            Property = property,
            GpCost = property.Internal.GoldCost,
            State = ChangeState.Added
        };

        _addedProperties.Add(entry);
    }

    public List<ChangelistEntry> ChangeList()
    {
        List<ChangelistEntry> changes = new();

        foreach (ChangelistEntry entry in _addedProperties)
        {
            changes.Add(entry);
        }

        foreach (ChangelistEntry entry in _removedProperties)
        {
            changes.Add(entry);
        }

        return changes;
    }

    public int TotalGpCost()
    {
        return _addedProperties.Sum(p => p.GpCost);
    }

    public void UndoRemoval(CraftingProperty craftingProperty)
    {
        ChangelistEntry? entry = _removedProperties.FirstOrDefault(p => p.Property == craftingProperty);

        if (entry == null) return;

        _removedProperties.Remove(entry);
    }

    public void UndoAddition(CraftingProperty craftingProperty)
    {
        ChangelistEntry? entry = _addedProperties.FirstOrDefault(p => p.Property == craftingProperty);

        if (entry == null) return;

        _addedProperties.Remove(entry);
    }

    public void UndoAllChanges()
    {
        _addedProperties.Clear();
        _removedProperties.Clear();
    }

    public class ChangelistEntry
    {
        public required string Label { get; set; }
        public required CraftingProperty Property { get; set; }
        public int GpCost { get; set; }
        public ChangeState State { get; set; }

        public ItemPropertyType BasePropertyType => Property.ItemProperty.Property.PropertyType;
    }
}
