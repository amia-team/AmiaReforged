using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to organization-related operations including creation, membership, and diplomacy.
/// </summary>
public interface IOrganizationSubsystem
{
    // === Organization Management ===

    /// <summary>
    /// Creates a new organization.
    /// </summary>
    Task<CommandResult> CreateOrganizationAsync(CreateOrganizationCommand command, CancellationToken ct = default);

    /// <summary>
    /// Disbands an existing organization.
    /// </summary>
    Task<CommandResult> DisbandOrganizationAsync(OrganizationId organizationId, CancellationToken ct = default);

    /// <summary>
    /// Updates organization details.
    /// </summary>
    Task<CommandResult> UpdateOrganizationAsync(OrganizationId organizationId, string? name = null, string? description = null, CancellationToken ct = default);

    // === Queries ===

    /// <summary>
    /// Gets organization details by ID.
    /// </summary>
    Task<IOrganization?> GetOrganizationDetailsAsync(GetOrganizationDetailsQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets all organizations a character is a member of.
    /// </summary>
    Task<List<OrganizationMember>> GetCharacterOrganizationsAsync(
        GetCharacterOrganizationsQuery query, CancellationToken ct = default);

    /// <summary>
    /// Gets all members of an organization.
    /// </summary>
    Task<List<OrganizationMember>> GetOrganizationMembersAsync(
        GetOrganizationMembersQuery query, CancellationToken ct = default);

    // === Membership Management ===

    /// <summary>
    /// Adds a character to an organization.
    /// </summary>
    Task<CommandResult> AddMemberAsync(OrganizationId organizationId, CharacterId characterId, string rank, CancellationToken ct = default);

    /// <summary>
    /// Removes a character from an organization.
    /// </summary>
    Task<CommandResult> RemoveMemberAsync(OrganizationId organizationId, CharacterId characterId, CancellationToken ct = default);

    /// <summary>
    /// Updates a member's rank within an organization.
    /// </summary>
    Task<CommandResult> UpdateMemberRankAsync(OrganizationId organizationId, CharacterId characterId, string newRank, CancellationToken ct = default);
}


