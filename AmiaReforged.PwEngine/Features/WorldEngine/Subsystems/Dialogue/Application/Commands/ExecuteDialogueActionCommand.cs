using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;

/// <summary>
/// Command to execute a dialogue action (e.g., start quest, give item) triggered by
/// entering a dialogue node or selecting a choice.
/// </summary>
public sealed record ExecuteDialogueActionCommand : ICommand
{
    /// <summary>
    /// The action to execute.
    /// </summary>
    public required DialogueAction Action { get; init; }

    /// <summary>
    /// The player the action applies to.
    /// </summary>
    public required NwPlayer Player { get; init; }

    /// <summary>
    /// The player's character ID.
    /// </summary>
    public required Guid CharacterId { get; init; }

    /// <summary>
    /// The NPC involved in the dialogue (for context).
    /// </summary>
    public required NwCreature Npc { get; init; }
}
