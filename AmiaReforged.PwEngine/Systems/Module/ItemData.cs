namespace AmiaReforged.PwEngine.Systems.Module;

public class ItemData
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public IEnumerable<ItemPropertyData>? ItemProperties { get; set; }
}