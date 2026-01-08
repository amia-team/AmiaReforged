using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Command to remove a member from a player stall.
/// Only the stall owner can remove members. The owner cannot be removed.
/// </summary>
public sealed record RemoveStallMemberCommand : ICommand
{
    public required long StallId { get; init; }
    public required string RequestorPersonaId { get; init; }
    public required string MemberPersonaId { get; init; }

    /// <summary>
    /// Creates a validated RemoveStallMemberCommand.
    /// </summary>
    public static RemoveStallMemberCommand Create(
        long stallId,
        string requestorPersonaId,
        string memberPersonaId)
    {
        if (stallId <= 0)
            throw new ArgumentException("Stall ID must be positive", nameof(stallId));

        if (string.IsNullOrWhiteSpace(requestorPersonaId))
            throw new ArgumentException("Requestor persona ID is required", nameof(requestorPersonaId));

        if (string.IsNullOrWhiteSpace(memberPersonaId))
            throw new ArgumentException("Member persona ID is required", nameof(memberPersonaId));

        return new RemoveStallMemberCommand
        {
            StallId = stallId,
            RequestorPersonaId = requestorPersonaId,
            MemberPersonaId = memberPersonaId
        };
    }
}
