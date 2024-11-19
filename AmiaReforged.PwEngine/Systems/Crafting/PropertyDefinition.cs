using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting;

public class PropertyDefinition
{
    public string Name { get; set; }
    public ItemProperty Property { get; set; }
    
    public PropertyDefinition(string name, ItemProperty property)
    {
        Name = name;
        Property = property;
    }
    
    
}