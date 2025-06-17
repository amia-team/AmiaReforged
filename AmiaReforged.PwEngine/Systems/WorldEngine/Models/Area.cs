using AmiaReforged.PwEngine.Systems.WorldEngine.Models.Economy;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public class Area
{
    public required string ResRef { get; set; }

    public NwArea? Reference { get; set; }

    public List<NodeDefinition> StaticNodes { get; set; } = new();

    public List<BreakableNodeDefinition> BreakableNodes { get; set; } = new();
}