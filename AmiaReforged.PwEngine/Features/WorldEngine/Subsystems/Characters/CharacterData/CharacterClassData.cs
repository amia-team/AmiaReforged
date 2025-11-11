namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

/// <summary>
/// Value object representing a character's class with its associated data.
/// Encapsulates class identity, level progression, and skills.
/// </summary>
/// <param name="Name">The class name (e.g., "Fighter", "Wizard")</param>
/// <param name="Levels">Number of levels in this class</param>
/// <param name="Skills">Skills associated with this class</param>
public record CharacterClassData(string Name, int Levels, IReadOnlyList<SkillData> Skills)
{
    /// <summary>
    /// Creates CharacterClassData with just name and levels (no skills).
    /// </summary>
    public static CharacterClassData From(string className, int levels) =>
        new(className, levels, Array.Empty<SkillData>());

    /// <summary>
    /// Creates CharacterClassData with name only (1 level, no skills).
    /// </summary>
    public static CharacterClassData From(string className) =>
        new(className, 1, Array.Empty<SkillData>());

    /// <summary>
    /// Implicit conversion from string to CharacterClassData for convenience.
    /// </summary>
    public static implicit operator CharacterClassData(string className) =>
        From(className);

    /// <summary>
    /// Returns the class name.
    /// </summary>
    public override string ToString() => Name;
}
