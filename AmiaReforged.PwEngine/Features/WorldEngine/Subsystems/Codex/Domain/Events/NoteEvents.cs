using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;

public sealed record NoteAddedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    Guid NoteId,
    string Content,
    NoteCategory Category,
    bool IsDmNote,
    bool IsPrivate
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record NoteEditedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    Guid NoteId,
    string NewContent
) : CodexDomainEvent(CharacterId, OccurredAt);

public sealed record NoteDeletedEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    Guid NoteId
) : CodexDomainEvent(CharacterId, OccurredAt);
