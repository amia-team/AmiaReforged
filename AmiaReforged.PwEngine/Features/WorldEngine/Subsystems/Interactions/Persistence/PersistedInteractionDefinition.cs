namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;

/// <summary>
/// EF Core entity for persisting interaction definitions to the database.
/// Maps to/from the domain <see cref="InteractionDefinition"/> class.
/// The responses list is stored as a JSONB column.
/// </summary>
public class PersistedInteractionDefinition
{
    /// <summary>Unique interaction tag. Primary key.</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Display name of the interaction type.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description for admin reference.</summary>
    public string? Description { get; set; }

    /// <summary>What kind of game entity this interaction targets (serialized enum name).</summary>
    public string TargetMode { get; set; } = "Trigger";

    /// <summary>Default number of rounds before completion.</summary>
    public int BaseRounds { get; set; } = 4;

    /// <summary>Minimum number of rounds even at maximum proficiency.</summary>
    public int MinRounds { get; set; } = 2;

    /// <summary>Whether proficiency reduces round count.</summary>
    public bool ProficiencyReducesRounds { get; set; } = true;

    /// <summary>Whether industry membership is required.</summary>
    public bool RequiresIndustryMembership { get; set; } = true;

    /// <summary>
    /// All responses for this interaction, stored as a JSONB array.
    /// </summary>
    public string ResponsesJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
