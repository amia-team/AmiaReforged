using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public class PersistentResource
{
    public BaseItemType ItemType { get; set; }
    public ItemType Type { get; set; }
    public string? NamingScheme { get; set; }
    public List<string> MaterialTags { get; set; } = [];
    public List<Material> Materials { get; set; } = [];
}