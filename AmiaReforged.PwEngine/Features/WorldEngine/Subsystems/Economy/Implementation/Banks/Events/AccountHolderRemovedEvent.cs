using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Events;

/// <summary>
/// Domain event published when an account holder is removed from a coinhouse account.
/// </summary>
public sealed record AccountHolderRemovedEvent : IDomainEvent
{
    public AccountHolderRemovedEvent(
        Guid accountId,
        CoinhouseTag coinhouseTag,
        Guid removedHolderId,
        string removedHolderName,
        HolderRole removedHolderRole,
        PersonaId removedBy)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        AccountId = accountId;
        CoinhouseTag = coinhouseTag;
        RemovedHolderId = removedHolderId;
        RemovedHolderName = removedHolderName;
        RemovedHolderRole = removedHolderRole;
        RemovedBy = removedBy;
    }

    /// <inheritdoc />
    public Guid EventId { get; }

    /// <inheritdoc />
    public DateTime OccurredAt { get; }

    /// <summary>
    /// The coinhouse account from which the holder was removed.
    /// </summary>
    public Guid AccountId { get; }

    /// <summary>
    /// The coinhouse where the account is held.
    /// </summary>
    public CoinhouseTag CoinhouseTag { get; }

    /// <summary>
    /// The ID of the holder that was removed.
    /// </summary>
    public Guid RemovedHolderId { get; }

    /// <summary>
    /// Display name of the removed holder for audit purposes.
    /// </summary>
    public string RemovedHolderName { get; }

    /// <summary>
    /// The role the holder had before removal.
    /// </summary>
    public HolderRole RemovedHolderRole { get; }

    /// <summary>
    /// The persona who initiated the removal.
    /// </summary>
    public PersonaId RemovedBy { get; }
}
