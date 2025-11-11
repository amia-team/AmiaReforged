using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.KnowledgeSubsystem;

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
}
