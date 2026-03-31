using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;

/// <summary>
/// Raised when a new dynamic quest posting is created from a template and made available.
/// </summary>
public sealed record QuestPostedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    PostingId PostingId,
    TemplateId TemplateId,
    string Title
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when a character claims a dynamic quest posting.
/// </summary>
public sealed record QuestClaimedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    PostingId PostingId,
    QuestId QuestId,
    TemplateId TemplateId,
    string Title,
    string Description,
    DateTime? Deadline
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when a claimant shares their dynamic quest with a party member for co-op play.
/// </summary>
public sealed record QuestSharedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    CharacterId InviteeId
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when a dynamic quest expires because its time limit elapsed.
/// </summary>
public sealed record QuestExpiredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    QuestId QuestId,
    ExpiryBehavior ExpiryBehavior
) : CodexDomainEvent(CharacterId, OccurredAt);

/// <summary>
/// Raised when a character drops their claim on a dynamic quest posting.
/// </summary>
public sealed record QuestUnclaimedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    PostingId PostingId,
    QuestId QuestId
) : CodexDomainEvent(CharacterId, OccurredAt);
