using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Events;

/// <summary>
/// Domain event published when an account holder's role is changed on a coinhouse account.
/// </summary>
public sealed record AccountHolderRoleChangedEvent : IDomainEvent
{
    public AccountHolderRoleChangedEvent(
        Guid accountId,
        CoinhouseTag coinhouseTag,
        Guid holderId,
        string holderName,
        HolderRole previousRole,
        HolderRole newRole,
        PersonaId changedBy)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        AccountId = accountId;
        CoinhouseTag = coinhouseTag;
        HolderId = holderId;
        HolderName = holderName;
        PreviousRole = previousRole;
        NewRole = newRole;
        ChangedBy = changedBy;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime OccurredAt { get; }

    /// <summary>
    /// The coinhouse account on which the role was changed.
    /// </summary>
    public Guid AccountId { get; }

    /// <summary>
    /// The coinhouse where the account is held.
    /// </summary>
    public CoinhouseTag CoinhouseTag { get; }

    /// <summary>
    /// The ID of the holder whose role was changed.
    /// </summary>
    public Guid HolderId { get; }

    /// <summary>
    /// Display name of the holder for audit purposes.
    /// </summary>
    public string HolderName { get; }

    /// <summary>
    /// The role the holder had before the change.
    /// </summary>
    public HolderRole PreviousRole { get; }

    /// <summary>
    /// The new role assigned to the holder.
    /// </summary>
    public HolderRole NewRole { get; }

    /// <summary>
    /// The persona who initiated the role change.
    /// </summary>
    public PersonaId ChangedBy { get; }
}
