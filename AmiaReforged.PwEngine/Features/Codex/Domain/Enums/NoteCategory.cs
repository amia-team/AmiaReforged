namespace AmiaReforged.PwEngine.Features.Codex.Domain.Enums;

/// <summary>
/// Represents the category/type of a note entry.
/// </summary>
public enum NoteCategory
{
    /// <summary>
    /// General personal note.
    /// </summary>
    General = 0,

    /// <summary>
    /// Quest-related note.
    /// </summary>
    Quest = 1,

    /// <summary>
    /// Character/NPC-related note.
    /// </summary>
    Character = 2,

    /// <summary>
    /// Location/area-related note.
    /// </summary>
    Location = 3,

    /// <summary>
    /// DM note visible to player.
    /// </summary>
    DmNote = 4,

    /// <summary>
    /// Private DM note (not visible to player).
    /// </summary>
    DmPrivate = 5
}
