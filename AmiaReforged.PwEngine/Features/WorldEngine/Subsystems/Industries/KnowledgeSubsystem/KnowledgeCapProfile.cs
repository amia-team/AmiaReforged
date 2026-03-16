namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// A named profile defining custom soft and hard caps for economy-earned knowledge points.
/// Profiles allow different character archetypes (e.g., "Artisan") to have different
/// progression limits without per-character manual overrides.
/// 
/// When a character has no profile assigned, global defaults apply (configurable via
/// the admin panel's Industries &gt; Knowledge Progression tab).
/// </summary>
public class KnowledgeCapProfile
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Unique tag identifier (e.g., "artisan", "scholar"). Used as FK from KnowledgeProgression.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Display name (e.g., "Artisan").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of when/why this profile is used.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Economy-earned KP threshold where progression becomes very slow/tedious.
    /// Beyond this point, costs are multiplied by the penalty multiplier.
    /// </summary>
    public int SoftCap { get; set; } = 100;

    /// <summary>
    /// Economy-earned KP hard limit. No more economy KP can be earned beyond this.
    /// </summary>
    public int HardCap { get; set; } = 150;
}
