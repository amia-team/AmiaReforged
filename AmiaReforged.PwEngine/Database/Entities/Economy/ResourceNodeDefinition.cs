using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Systems.WorldEngine;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.HarvestActions;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class ResourceNodeDefinition
{
    /// <summary>
    /// Sets the tag so that the definition may be uniquely identified
    /// </summary>
    [Key] public required string Tag { get; set; }

    /// <summary>
    /// Required name field. What the player sees when they mouse over instances of this resource node's placeables.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional field for setting a description for job system nodes.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Appearance.2da entry that determines how the node looks in game
    /// </summary>
    public required int Appearance { get; set; }

    /// <summary>
    /// Optional variation in size to create a more varied look to duplicate nodes.
    /// </summary>
    public float ScaleVariance { get; set; } = 0.0f;

    /// <summary>
    /// Amount of time in rounds it takes to complete one harvest cycle.
    /// </summary>
    public required int HarvestTime { get; set; }

    public required ResourceType Type { get; set; }

    public required HarvestActionEnum HarvestAction { get; set; }
}
