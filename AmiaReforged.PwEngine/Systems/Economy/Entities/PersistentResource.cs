using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Economy.Entities;

public class PersistentResource
{
    public BaseItemType ItemType { get; set; }
    public List<Material> Materials { get; set; }
    public int Quantity { get; set; }
}