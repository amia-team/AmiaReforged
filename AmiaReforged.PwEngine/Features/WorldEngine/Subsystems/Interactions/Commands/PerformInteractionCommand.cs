using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;

/// <summary>
/// Command to start or continue an interaction. Each invocation represents one "tick":
/// <list type="bullet">
///   <item>If the character has no active session for this tag+target, a new session is created.</item>
///   <item>If the character already has a matching session, progress is advanced.</item>
///   <item>If the character has an active session for a <em>different</em> interaction,
///         the old session is cancelled and a new one starts.</item>
/// </list>
/// </summary>
public sealed record PerformInteractionCommand(
    CharacterId CharacterId,
    string InteractionTag,
    Guid TargetId,
    string? AreaResRef = null,
    Dictionary<string, object>? Metadata = null) : ICommand;
