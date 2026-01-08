using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Command to add a new member to a player stall.
/// Only the stall owner can add members. New members receive all permissions by default.
/// </summary>
public sealed record AddStallMemberCommand : ICommand
{
    public required long StallId { get; init; }
    public required string RequestorPersonaId { get; init; }
    public required string MemberPersonaId { get; init; }
    public required string MemberDisplayName { get; init; }
    public bool CanManageInventory { get; init; } = true;
    public bool CanConfigureSettings { get; init; } = true;
    public bool CanCollectEarnings { get; init; } = true;

    /// <summary>
    /// Creates a validated AddStallMemberCommand.
    /// </summary>
    public static AddStallMemberCommand Create(
        long stallId,
        string requestorPersonaId,
        string memberPersonaId,
        string memberDisplayName,
        bool canManageInventory = true,
        bool canConfigureSettings = true,
        bool canCollectEarnings = true)
    {
        if (stallId <= 0)
            throw new ArgumentException("Stall ID must be positive", nameof(stallId));

        if (string.IsNullOrWhiteSpace(requestorPersonaId))
            throw new ArgumentException("Requestor persona ID is required", nameof(requestorPersonaId));

        if (string.IsNullOrWhiteSpace(memberPersonaId))
            throw new ArgumentException("Member persona ID is required", nameof(memberPersonaId));

        if (string.IsNullOrWhiteSpace(memberDisplayName))
            throw new ArgumentException("Member display name is required", nameof(memberDisplayName));

        return new AddStallMemberCommand
        {
            StallId = stallId,
            RequestorPersonaId = requestorPersonaId,
            MemberPersonaId = memberPersonaId,
            MemberDisplayName = memberDisplayName,
            CanManageInventory = canManageInventory,
            CanConfigureSettings = canConfigureSettings,
            CanCollectEarnings = canCollectEarnings
        };
    }
}
