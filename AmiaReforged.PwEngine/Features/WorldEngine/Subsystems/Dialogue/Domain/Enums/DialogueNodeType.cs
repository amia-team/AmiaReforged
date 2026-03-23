namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

/// <summary>
/// Defines the type of a dialogue node within a conversation tree.
/// </summary>
public enum DialogueNodeType
{
    /// <summary>
    /// The root/entry point of the dialogue tree. Each tree has exactly one.
    /// </summary>
    Root,

    /// <summary>
    /// An NPC speaks text to the player. May have multiple player choices branching from it.
    /// </summary>
    NpcText,

    /// <summary>
    /// A node that fires actions (e.g., accept quest, give item) without displaying text.
    /// Automatically transitions to the next node.
    /// </summary>
    Action,

    /// <summary>
    /// Terminates the conversation. Can optionally fire final actions.
    /// </summary>
    End
}
