using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// Persistent representation of an <see cref="Organizations.OrganizationMember"/>.
/// </summary>
[Table("organization_members")]
public class OrganizationMemberRecord
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("character_id")]
    public Guid CharacterId { get; set; }

    [Column("organization_id")]
    public Guid OrganizationId { get; set; }

    [Column("rank")]
    public OrganizationRank Rank { get; set; }

    [Column("status")]
    public MembershipStatus Status { get; set; }

    [Column("joined_date")]
    public DateTime JoinedDate { get; set; }

    [Column("departed_date")]
    public DateTime? DepartedDate { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    /// <summary>
    /// JSON serialized list of role strings.
    /// </summary>
    [Column("roles", TypeName = "jsonb")]
    public string RolesJson { get; set; } = "[]";

    /// <summary>
    /// JSON serialized metadata object.
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string MetadataJson { get; set; } = "{}";
}
