using System.Runtime.Serialization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Models.Economy;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

public interface IEventBus;

public interface IAmiaEntity;

public interface IAmiaEvent;

public class MeleeHarvestEvent : IAmiaEvent
{
    public required WorldCharacter Attacker { get; set; }

    public required BreakableNodeDefinition Attacked { get; set; }

    public Tool? ToolUsed { get; set; }
}

public class Tool
{
    public required ToolEnum TypeOfTool { get; set; }
    public required QualityEnum Quality { get; set; }
}

public class BreakableNodeDefinition : NodeDefinition
{
    public float BrokenPercentage { get; set; }
    public List<RawGood> Goods { get; set; } = [];
}

public class RawGood
{
}

public class WorldCharacter : IAmiaEntity
{
}

[ServiceBinding(typeof(WorldEventBus))]
public class WorldEventBus
{
}