using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;

/// <summary>
/// Command to change a member's rank within an organization
/// </summary>
public record ChangeRankCommand : ICommand
{
    public required OrganizationId OrganizationId { get; init; }
    public required CharacterId CharacterId { get; init; }
    public required OrganizationRank NewRank { get; init; }
    public required CharacterId ChangedBy { get; init; }
}

/// <summary>
/// Handles changing member ranks in organizations
/// </summary>
public class ChangeRankHandler : ICommandHandler<ChangeRankCommand>
{
    private readonly IOrganizationMemberRepository _memberRepository;
    private readonly IEventBus _eventBus;

    public ChangeRankHandler(IOrganizationMemberRepository memberRepository, IEventBus eventBus)
    {
        _memberRepository = memberRepository;
        _eventBus = eventBus;
    }

    public Task<CommandResult> HandleAsync(ChangeRankCommand command, CancellationToken cancellationToken = default)
    {
        // Get the member to change rank
        OrganizationMember? member = _memberRepository.GetByCharacterAndOrganization(
            command.CharacterId,
            command.OrganizationId);

        if (member == null || member.Status != MembershipStatus.Active)
        {
            return Task.FromResult(CommandResult.Fail("Member not found or not active"));
        }

        // Get the person making the change
        OrganizationMember? changer = _memberRepository.GetByCharacterAndOrganization(
            command.ChangedBy,
            command.OrganizationId);

        if (changer == null || !changer.CanManageMembers())
        {
            return Task.FromResult(CommandResult.Fail("Insufficient permissions to change member ranks"));
        }

        // Leaders can only be changed by other leaders
        if (member.Rank == OrganizationRank.Leader && changer.Rank != OrganizationRank.Leader)
        {
            return Task.FromResult(CommandResult.Fail("Only leaders can change leader ranks"));
        }

        // Can't promote to a rank higher than your own
        if (command.NewRank > changer.Rank)
        {
            return Task.FromResult(CommandResult.Fail("Cannot promote members to a rank higher than your own"));
        }

        // If no change, return early
        if (member.Rank == command.NewRank)
        {
            return Task.FromResult(CommandResult.Fail($"Member already has rank: {command.NewRank}"));
        }

        // Store previous rank
        OrganizationRank previousRank = member.Rank;

        // Change the rank
        member.Rank = command.NewRank;
        _memberRepository.Update(member);
        _memberRepository.SaveChanges();

        // Publish event
        MemberRoleChangedEvent evt = new(
            command.CharacterId,
            command.OrganizationId,
            command.NewRank,
            previousRank,
            DateTime.UtcNow);
        _eventBus.PublishAsync(evt).GetAwaiter().GetResult();

        return Task.FromResult(CommandResult.Ok());
    }
}

