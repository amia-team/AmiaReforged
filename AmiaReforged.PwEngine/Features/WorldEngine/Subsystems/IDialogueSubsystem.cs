using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Subsystem interface for the dialogue/conversation system.
/// Provides access to dialogue management and runtime conversation control.
/// </summary>
public interface IDialogueSubsystem
{
    /// <summary>
    /// Starts a dialogue conversation between a player and an NPC.
    /// </summary>
    Task<bool> StartDialogueAsync(NwPlayer player, NwCreature npc, DialogueTreeId treeId, Guid characterId);

    /// <summary>
    /// Ends the active dialogue for a player.
    /// </summary>
    void EndDialogue(NwPlayer player, string reason = "goodbye");

    /// <summary>
    /// Gets all dialogue trees available for an NPC by its speaker tag.
    /// </summary>
    Task<List<DialogueTree>> GetDialoguesForNpcAsync(string npcTag);

    /// <summary>
    /// Checks if a player has an active dialogue session.
    /// </summary>
    bool HasActiveDialogue(NwPlayer player);
}
