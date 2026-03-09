namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Tracks the lifecycle state of an <see cref="InteractionSession"/>.
/// </summary>
public enum InteractionStatus
{
    /// <summary>The interaction is in progress; more ticks are needed.</summary>
    Active,

    /// <summary>The interaction finished successfully.</summary>
    Completed,

    /// <summary>The interaction was cancelled before completion (e.g., player started a different interaction).</summary>
    Cancelled,

    /// <summary>The interaction failed due to a runtime condition (e.g., node despawned mid-harvest).</summary>
    Failed
}
