using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

/// <summary>
/// Contextual data passed to <see cref="IPrecondition"/> checks and
/// <see cref="IInteractionHandler"/> methods, describing the interaction attempt.
/// </summary>
public record InteractionContext(
    CharacterId CharacterId,
    Guid TargetId,
    InteractionTargetMode TargetMode,
    string? AreaResRef = null,
    Dictionary<string, object>? Metadata = null);
