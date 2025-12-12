using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.MythalCategory;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;

/// <summary>
/// Represents a model for managing changes in crafting properties during the crafting process.
/// This model tracks additions and removals of properties and calculates the associated costs.
/// </summary>
public class ChangeListModel
{
    /// <summary>
    /// Represents the state of a change made in the change list model.
    /// This enumeration is used to denote whether a specific change
    /// has been added or removed.
    /// </summary>
    public enum ChangeState
    {
        /// <summary>
        /// Represents a state change where an item or property has been added.
        /// </summary>
        Added,

        /// <summary>
        /// Indicates that an item property has been marked for removal in the changelist.
        /// This state is used to track properties that are removed from an item during a crafting or modification process.
        /// </summary>
        Removed
    }

    /// <summary>
    /// A private collection that maintains a list of properties marked as newly added
    /// during the current crafting session. These properties represent changes identified
    /// by the state <see cref="ChangeListModel.ChangeState.Added"/>.
    /// </summary>
    /// <remarks>
    /// The list is used to track newly introduced crafting properties. Each entry in the list
    /// is represented as a <see cref="ChangeListModel.ChangelistEntry"/> and includes details
    /// such as the property's label, gold cost, and associated state.
    /// The collection provides critical data for managing and reverting crafting changes.
    /// </remarks>
    private readonly List<ChangelistEntry> _addedProperties = new();

    /// <summary>
    /// A private list that holds the collection of properties marked for removal as part of the change list.
    /// Used internally to track and manage the properties slated for removal from the current crafting operation.
    /// </summary>
    private readonly List<ChangelistEntry> _removedProperties = new();

    /// <summary>
    /// Adds a property to the list of removed properties in the change list.
    /// </summary>
    /// <param name="property">The crafting property to be marked as removed and added to the change list.</param>
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

    /// <summary>
    /// Adds a new property to the change list and marks it as added.
    /// </summary>
    /// <param name="property">The property to add, including its associated metadata such as label and cost.</param>
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

    /// Retrieves a consolidated list of all changes that have been added or removed.
    /// <returns>
    /// A list of ChangelistEntry objects representing the added and removed properties.
    /// </returns>
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

    /// <summary>
    /// Calculates the total gold piece (GP) cost of all the added properties in the changelist.
    /// </summary>
    /// <returns>The total GP cost of the added properties.</returns>
    public int TotalGpCost()
    {
        return _addedProperties.Sum(p => p.GpCost);
    }

    /// <summary>
    /// Reverts the removal of a specific crafting property by updating the internal removed properties list.
    /// </summary>
    /// <param name="craftingProperty">The crafting property that should be restored from the removed state.</param>
    public void UndoRemoval(CraftingProperty craftingProperty)
    {
        ChangelistEntry? entry = _removedProperties.FirstOrDefault(p => p.Property == craftingProperty);

        if (entry == null) return;

        _removedProperties.Remove(entry);
    }

    /// <summary>
    /// Removes a property that was previously added to the change list. If the property does not exist in the list of added properties, no action is taken.
    /// </summary>
    /// <param name="craftingProperty">The crafting property to be removed from the list of added properties.</param>
    public void UndoAddition(CraftingProperty craftingProperty)
    {
        ChangelistEntry? entry = _addedProperties.FirstOrDefault(p => p.Property == craftingProperty);

        if (entry == null) return;

        _addedProperties.Remove(entry);
    }

    /// <summary>
    /// Resets all changes tracked by the change list model.
    /// </summary>
    /// <remarks>
    /// This method clears both the list of added properties and the list of removed properties,
    /// effectively undoing all previously recorded changes.
    /// </remarks>
    public void UndoAllChanges()
    {
        _addedProperties.Clear();
        _removedProperties.Clear();
    }

    /// <summary>
    /// Represents an individual entry in the changelist for a crafting operation.
    /// </summary>
    public class ChangelistEntry
    {
        /// <summary>
        /// Gets or sets the label describing the corresponding property in the change list entry.
        /// This label is used to visually identify the property within the context of the change list,
        /// such as when it is displayed in the user interface or processed within the crafting system.
        /// </summary>
        public required string Label { get; set; }

        /// <summary>
        /// Gets or sets the crafting property associated with the changelist entry. This property
        /// represents the specific attribute or characteristic that is being modified within the
        /// change list context, such as additions, removals, or updates to crafting properties.
        /// </summary>
        public required CraftingProperty Property { get; set; }

        /// <summary>
        /// Represents the gold cost associated with a specific crafting property within a changelist entry.
        /// </summary>
        /// <remarks>
        /// The gold cost is derived from the corresponding crafting property and indicates the amount of in-game currency
        /// required to apply or remove the property.
        /// </remarks>
        public int GpCost { get; set; }


        /// <summary>
        /// Represents the current state of a change in the crafting process.
        /// </summary>
        /// <remarks>
        /// This property indicates whether a property was added or removed in the context
        /// of the crafting system's changelist functionality.
        /// </remarks>
        public ChangeState State { get; set; }

        /// <summary>
        /// Gets the type of the base property associated with the current <see cref="CraftingProperty"/>.
        /// This represents the fundamental item property type as defined in the system.
        /// </summary>
        public ItemPropertyType BasePropertyType => Property.ItemProperty.Property.PropertyType;
    }
}
