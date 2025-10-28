using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

/// <summary>
/// Represents a diplomatic relationship between two organizations
/// </summary>
public class DiplomaticRelation
{
    /// <summary>
    /// Unique identifier for this relationship
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The organization initiating/owning this relationship
    /// </summary>
    public required OrganizationId SourceOrganizationId { get; init; }

    /// <summary>
    /// The target organization
    /// </summary>
    public required OrganizationId TargetOrganizationId { get; init; }

    /// <summary>
    /// The diplomatic stance
    /// </summary>
    public required DiplomaticStance Stance { get; set; }

    /// <summary>
    /// When this relationship was established
    /// </summary>
    public DateTime EstablishedDate { get; init; }

    /// <summary>
    /// When the relationship was last modified
    /// </summary>
    public DateTime LastModifiedDate { get; set; }

    /// <summary>
    /// Treaties or agreements in place
    /// </summary>
    public List<string> Treaties { get; init; } = [];

    /// <summary>
    /// Optional notes about this relationship
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Extensible metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Check if this is a positive relationship
    /// </summary>
    public bool IsPositive() => Stance >= DiplomaticStance.Friendly;

    /// <summary>
    /// Check if this is a negative relationship
    /// </summary>
    public bool IsNegative() => Stance < DiplomaticStance.Neutral;

    /// <summary>
    /// Check if organizations are at war
    /// </summary>
    public bool AtWar() => Stance == DiplomaticStance.War;
}


