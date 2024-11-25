using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting;

public class PropertyDefinition
{
    /// <summary>
    /// Name of the category to display in the UI
    /// </summary>
    public string Category { get; set; }

    /// <summary>
    /// Dictionary of item properties to link to the UI.
    /// </summary>
    public Dictionary<string, ItemProperty> Properties { get; set; }

    /// <summary>
    /// Overrides the default display of the properties in a flat structure, avoiding a combo box.
    /// </summary>
    public bool FlatStructure { get; set; }

    /// <summary>
    /// A list of item constants from the static NWScript domain (or the 2da file row number) that this property definition supports.
    /// </summary>
    public List<int> SupportedItemTypes { get; set; }

    /// <summary>
    /// Takes a category and a dictionary of properties to define a property definition.
    /// </summary>
    /// <param name="category">display name for a category.</param>
    /// <param name="properties">A dictionary tying a name of a property to its direct property</param>
    /// <param name="flatStructure">determines whether this property uses a combo box</param>
    /// <param name="supportedItemTypes">the list of items that can have this property</param>
    public PropertyDefinition(string category, Dictionary<string, ItemProperty> properties, bool flatStructure, List<int> supportedItemTypes)
    {
        Category = category;
        Properties = properties;
        FlatStructure = flatStructure;
        SupportedItemTypes = supportedItemTypes;
    }
}