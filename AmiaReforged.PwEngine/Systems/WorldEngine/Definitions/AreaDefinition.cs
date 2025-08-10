using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;

public class AreaDefinition
{
    public required string ResRef { get; set; }
    public string? SettlementReference { get; set; }

    public List<string> SpawnableNodes { get; set; } = [];

}
