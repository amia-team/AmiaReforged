namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Determines what kind of entity the interaction targets.
/// </summary>
public enum InteractionTargetMode
{
    /// <summary>
    /// Interaction targets a specific <see cref="ResourceNodes.ResourceNodeData.ResourceNodeInstance"/>.
    /// Example: Harvesting an ore vein.
    /// </summary>
    Node,

    /// <summary>
    /// Interaction targets a trigger zone within an area.
    /// Example: Prospecting a resource-rich cave section.
    /// </summary>
    Trigger,

    /// <summary>
    /// Interaction targets a specific placeable object.
    /// Example: Using a workstation to smelt ore.
    /// </summary>
    Placeable
}
