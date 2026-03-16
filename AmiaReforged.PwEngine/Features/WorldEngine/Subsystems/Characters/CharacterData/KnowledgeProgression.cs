namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

/// <summary>
/// Tracks a character's knowledge point progression through the economy system.
/// Knowledge points are a global per-character resource used across all industries.
/// 
/// Characters earn KP in two ways:
/// <list type="bullet">
///   <item><b>Level-up KP</b>: 1 KP granted per character level (1–30), given instantly. These do NOT
///   affect the economy progression curve.</item>
///   <item><b>Economy-earned KP</b>: Earned by accumulating progression points from crafting,
///   harvesting, and other economy activities. The cost per KP escalates via a configurable
///   curve and is subject to soft and hard caps.</item>
/// </list>
/// </summary>
public class KnowledgeProgression
{
    /// <summary>
    /// Primary key.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The character this progression belongs to (unique — one per character).
    /// </summary>
    public Guid CharacterId { get; init; }

    /// <summary>
    /// Knowledge points earned through the economy progression system.
    /// Subject to soft/hard caps and the escalating cost curve.
    /// </summary>
    public int EconomyEarnedKnowledgePoints { get; set; }

    /// <summary>
    /// Knowledge points granted directly from character level-ups (max 30).
    /// These are free bonuses that do NOT inflate the economy progression curve.
    /// </summary>
    public int LevelUpKnowledgePoints { get; set; }

    /// <summary>
    /// Accumulated progression points toward the next economy-earned KP.
    /// When this reaches the threshold for the next KP, it rolls over.
    /// </summary>
    public int AccumulatedProgressionPoints { get; set; }

    /// <summary>
    /// Optional tag referencing a <see cref="KnowledgeCapProfile"/> for cap overrides.
    /// Null means use the global default caps.
    /// </summary>
    public string? CapProfileTag { get; set; }

    /// <summary>
    /// Total knowledge points available (economy-earned + level-up).
    /// This is the pool from which points are spent on learning knowledge nodes.
    /// </summary>
    public int TotalKnowledgePoints => EconomyEarnedKnowledgePoints + LevelUpKnowledgePoints;
}
