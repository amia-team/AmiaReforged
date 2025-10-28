using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

/// <summary>
/// Command to assign a role to a member
/// </summary>
public record AssignRoleCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required MemberRole Role { get; init; }
    public required CharacterId AssignedBy { get; init; }
}

/// <summary>
/// Handles assigning roles to organization members
/// </summary>
public class AssignRoleHandler : ICommandHandler<AssignRoleCommand>
{
    private readonly IOrganizationMemberRepository _memberRepository;

    public AssignRoleHandler(IOrganizationMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public Task<CommandResult> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        // Get the member to assign role to
        var member = _memberRepository.GetByCharacterAndOrganization(
            command.CharacterId,
            command.OrganizationId);

        if (member == null || member.Status != MembershipStatus.Active)
        {
            return Task.FromResult(CommandResult.Fail("Member not found or not active"));
        }

        // Check permissions
        var assigner = _memberRepository.GetByCharacterAndOrganization(
            command.AssignedBy,
            command.OrganizationId);

        if (assigner == null || !assigner.CanManageMembers())
        {
            return Task.FromResult(CommandResult.Fail("Insufficient permissions to assign roles"));
        }

        // Check if role already assigned
        if (member.HasRole(command.Role))
        {
            return Task.FromResult(CommandResult.Fail($"Member already has role: {command.Role}"));
        }

        // Assign the role
        member.Roles.Add(command.Role);
        _memberRepository.Update(member);
        _memberRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }
}

/// <summary>
/// Command to revoke a role from a member
/// </summary>
public record RevokeRoleCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required MemberRole Role { get; init; }
    public required CharacterId RevokedBy { get; init; }
}

/// <summary>
/// Handles revoking roles from organization members
/// </summary>
public class RevokeRoleHandler : ICommandHandler<RevokeRoleCommand>
{
    private readonly IOrganizationMemberRepository _memberRepository;

    public RevokeRoleHandler(IOrganizationMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public Task<CommandResult> HandleAsync(RevokeRoleCommand command, CancellationToken cancellationToken = default)
    {
        // Get the member
        var member = _memberRepository.GetByCharacterAndOrganization(
            command.CharacterId,
            command.OrganizationId);

        if (member == null || member.Status != MembershipStatus.Active)
        {
            return Task.FromResult(CommandResult.Fail("Member not found or not active"));
        }

        // Check permissions
        var revoker = _memberRepository.GetByCharacterAndOrganization(
            command.RevokedBy,
            command.OrganizationId);

        if (revoker == null || !revoker.CanManageMembers())
        {
            return Task.FromResult(CommandResult.Fail("Insufficient permissions to revoke roles"));
        }

        // Check if member has the role
        if (!member.HasRole(command.Role))
        {
            return Task.FromResult(CommandResult.Fail($"Member does not have role: {command.Role}"));
        }

        // Revoke the role
        member.Roles.RemoveAll(r => r.Value == command.Role.Value);
        _memberRepository.Update(member);
        _memberRepository.SaveChanges();

        return Task.FromResult(CommandResult.Ok());
    }
}

