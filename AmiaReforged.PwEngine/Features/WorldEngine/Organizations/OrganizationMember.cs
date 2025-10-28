using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

/// <summary>
/// Represents a character's membership in an organization
/// </summary>
public class OrganizationMember
{
    /// <summary>
    /// Unique identifier for this membership
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The character who is a member
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// The organization they belong to
    /// </summary>
    public required OrganizationId OrganizationId { get; init; }

    /// <summary>
    /// Current rank in the organization
    /// </summary>
    public required OrganizationRank Rank { get; set; }

    /// <summary>
    /// Custom roles assigned to this member
    /// </summary>
    public List<MemberRole> Roles { get; init; } = [];

    /// <summary>
    /// When the member joined
    /// </summary>
    public DateTime JoinedDate { get; init; }

    /// <summary>
    /// Current membership status
    /// </summary>
    public required MembershipStatus Status { get; set; }

    /// <summary>
    /// Optional notes about this membership
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the membership ended (if Status is Departed/Expelled/Banned)
    /// </summary>
    public DateTime? DepartedDate { get; set; }

    /// <summary>
    /// Extensible metadata for organization-specific data
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Check if member has a specific role
    /// </summary>
    public bool HasRole(MemberRole role) => Roles.Any(r => r.Value == role.Value);

    /// <summary>
    /// Check if member can manage other members (Officer or higher)
    /// </summary>
    public bool CanManageMembers() => Rank >= OrganizationRank.Officer;

    /// <summary>
    /// Check if member is leader
    /// </summary>
    public bool IsLeader() => Rank == OrganizationRank.Leader;
}

