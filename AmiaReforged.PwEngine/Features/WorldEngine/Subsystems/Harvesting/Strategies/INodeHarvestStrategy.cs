using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Strategies;

/// <summary>
/// Strategy interface for type-specific resource node harvesting behavior.
/// Each implementation owns the full lifecycle: event wiring, progress tracking,
/// output calculation, and cleanup for a set of <see cref="ResourceType"/>s.
/// </summary>
public interface INodeHarvestStrategy
{
    /// <summary>
    /// The set of <see cref="ResourceType"/>s this strategy handles.
    /// </summary>
    IReadOnlySet<ResourceType> SupportedTypes { get; }

    /// <summary>
    /// Wire NWN events on <paramref name="placeable"/> so this strategy handles
    /// player interactions (attacks, use, etc.) for the given <paramref name="node"/>.
    /// </summary>
    void WireEvents(NwPlaceable placeable, SpawnedNode node);

    /// <summary>
    /// Remove all NWN event handlers previously attached by <see cref="WireEvents"/>.
    /// Called during area cleanup before the placeable is destroyed.
    /// </summary>
    void UnwireEvents(NwPlaceable placeable);
}
