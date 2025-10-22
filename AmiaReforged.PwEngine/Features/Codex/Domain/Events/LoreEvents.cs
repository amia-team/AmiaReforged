using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.Codex.Domain.Events;

public sealed record LoreDiscoveredEvent(
    CharacterId CharacterId,
    DateTime OccurredAt,
    LoreId LoreId,
    string Title,
    string Summary,
    string Source,
    LoreTier Tier,
    IReadOnlyList<Keyword> Keywords
) : CodexDomainEvent(CharacterId, OccurredAt);
