using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

/// <summary>
/// Command to add a member to an organization
/// </summary>
public record AddMemberCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public required CharacterId CharacterId { get; init; }
    public OrganizationRank InitialRank { get; init; } = OrganizationRank.Recruit;
    public List<MemberRole> InitialRoles { get; init; } = [];
}

/// <summary>
/// Handles adding members to organizations
/// </summary>
public class AddMemberHandler : ICommandHandler<AddMemberCommand>
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IOrganizationRepository _organizationRepository;

    public AddMemberHandler(
        IOrganizationMemberRepository memberRepository,
        IOrganizationRepository organizationRepository)
    {
        _memberRepository = memberRepository;
        _organizationRepository = organizationRepository;
    }

    public Task<CommandResult> HandleAsync(AddMemberCommand command, CancellationToken cancellationToken = default)
    {
        // Validate organization exists
        IOrganization? organization = _organizationRepository.GetById(command.OrganizationId);
        if (organization == null)
        {
            return Task.FromResult(CommandResult.Fail($"Organization not found: {command.OrganizationId}"));
        }

        // Check if banned from organization
        if (organization.BanList.Contains(command.CharacterId))
        {
            return Task.FromResult(CommandResult.Fail("Character is banned from this organization"));
        }

        // Check if already a member
        OrganizationMember? existingMembership = _memberRepository.GetByCharacterAndOrganization(
            command.CharacterId,
            command.OrganizationId);

        if (existingMembership != null && existingMembership.Status == MembershipStatus.Active)
        {
            return Task.FromResult(CommandResult.Fail("Character is already an active member of this organization"));
        }

        // Check if has banned status
        if (existingMembership?.Status == MembershipStatus.Banned)
        {
            return Task.FromResult(CommandResult.Fail("Character is banned from this organization"));
        }

        // Create new membership
        OrganizationMember membership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = command.CharacterId,
            OrganizationId = command.OrganizationId,
            Rank = command.InitialRank,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = command.InitialRoles
        };

        _memberRepository.Add(membership);
        _memberRepository.SaveChanges();

        return Task.FromResult(CommandResult.OkWith("MembershipId", membership.Id));
    }
}
/// <summary>

