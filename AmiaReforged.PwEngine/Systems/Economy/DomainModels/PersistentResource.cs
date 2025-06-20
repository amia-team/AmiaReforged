using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Economy.DomainModels;

public class PersistentResource
{
    public BaseItemType ItemType { get; set; }
    public ItemType Type { get; set; }
    public string? NamingScheme { get; set; }
    public List<string> MaterialTags { get; set; } = [];
    public List<MaterialDefinition> Materials { get; set; } = [];
}