namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

public class Knowledge
{
    /// <summary>
    /// Uniquely identifies an article of knowledge in the system.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// String used when displaying knowledge to the user.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// String used when displaying what knowledge does to a user
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Determines what Industry rank is needed to learn this knowledge
    /// </summary>
    public required ProficiencyLevel Level { get; init; }

    /// <summary>
    /// List of effects that define what specific types of resource nodes are impacted by this knowledge
    /// </summary>
    public List<KnowledgeHarvestEffect> HarvestEffects { get; set; } = [];

    /// <summary>
    /// Immutable value that determines how many knowledge points are required to learn this information.
    /// </summary>
    public int PointCost { get; init; }

    /// <summary>
    /// Knowledge tags that must be learned before this knowledge can be acquired.
    /// Forms a directed acyclic graph (DAG) within the industry.
    /// </summary>
    public List<string> Prerequisites { get; init; } = [];

    /// <summary>
    /// Optional named branch for specialization grouping (e.g., "Bladesmith", "Armorsmith").
    /// Characters can explore different branches within the same industry.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Effects triggered when this knowledge is learned — bridges to other subsystems
    /// (unlock recipes, grant codex entries, modify harvest, etc.).
    /// </summary>
    public List<KnowledgeEffect> Effects { get; init; } = [];
}
