using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Events;

/// <summary>
/// Raised when a dialogue conversation begins between a player and an NPC.
/// </summary>
public sealed record DialogueStartedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required DialogueTreeId DialogueTreeId { get; init; }
    public required Guid CharacterId { get; init; }
    public required string NpcTag { get; init; }
}

/// <summary>
/// Raised when the conversation advances to a new node.
/// </summary>
public sealed record DialogueNodeEnteredEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required DialogueTreeId DialogueTreeId { get; init; }
    public required DialogueNodeId NodeId { get; init; }
    public required Guid CharacterId { get; init; }
}

/// <summary>
/// Raised when a player selects a dialogue choice.
/// Can be used to emit a QuestSignal with SignalType.DialogChoice.
/// </summary>
public sealed record DialogueChoiceMadeEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required DialogueTreeId DialogueTreeId { get; init; }
    public required DialogueNodeId FromNodeId { get; init; }
    public required DialogueNodeId ToNodeId { get; init; }
    public required int ChoiceIndex { get; init; }
    public required string ChoiceText { get; init; }
    public required Guid CharacterId { get; init; }
}

/// <summary>
/// Raised when a dialogue conversation ends (player said goodbye, walked away, or reached an End node).
/// </summary>
public sealed record DialogueEndedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required DialogueTreeId DialogueTreeId { get; init; }
    public required Guid CharacterId { get; init; }
    public required string Reason { get; init; }
}
