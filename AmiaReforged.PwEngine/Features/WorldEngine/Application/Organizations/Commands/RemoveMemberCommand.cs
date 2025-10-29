using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

/// <summary>
/// Command to remove a member from an organization
/// </summary>
public record RemoveMemberCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required CharacterId RemovedBy { get; init; }
    public string? Reason { get; init; }
    public bool IsBan { get; init; } = false;
}

/// <summary>
/// Handles removing members from organizations
/// </summary>
public class RemoveMemberHandler : ICommandHandler<RemoveMemberCommand>
{
    private readonly IOrganizationMemberRepository _memberRepository;

    public RemoveMemberHandler(IOrganizationMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public Task<CommandResult> HandleAsync(RemoveMemberCommand command, CancellationToken cancellationToken = default)
    {
        // Get the membership to remove
        OrganizationMember? membership = _memberRepository.GetByCharacterAndOrganization(
            command.CharacterId,
            command.OrganizationId);

        if (membership == null)
        {
            return Task.FromResult(CommandResult.Fail("Member not found in organization"));
        }

        if (membership.Status != MembershipStatus.Active)
        {
            return Task.FromResult(CommandResult.Fail($"Member is not active (current status: {membership.Status})"));
        }

        // Check permissions - get the remover's membership
        OrganizationMember? removerMembership = _memberRepository.GetByCharacterAndOrganization(
            command.RemovedBy,
            command.OrganizationId);

        if (removerMembership == null)
        {
            return Task.FromResult(CommandResult.Fail("Remover is not a member of this organization"));
        }

        // Can't remove someone of equal or higher rank (unless removing self)
        if (command.CharacterId != command.RemovedBy)
        {
            if (!removerMembership.CanManageMembers())
            {
                return Task.FromResult(CommandResult.Fail("Insufficient permissions to remove members"));
            }

            if (membership.Rank >= removerMembership.Rank)
            {
                return Task.FromResult(CommandResult.Fail("Cannot remove members of equal or higher rank"));
            }
        }

        // Update membership status
        MembershipStatus newStatus;
        if (command.IsBan)
        {
            newStatus = MembershipStatus.Banned;
        }
        else if (command.CharacterId == command.RemovedBy)
        {
            // Self-removal is voluntary departure
            newStatus = MembershipStatus.Departed;
        }
        else
        {
            // Removed by someone else is expulsion
            newStatus = MembershipStatus.Expelled;
        }

        membership.Status = newStatus;
        membership.DepartedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(command.Reason))
        {
            membership.Notes = command.Reason;
        }

        _memberRepository.Update(membership);
        _memberRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }
}

