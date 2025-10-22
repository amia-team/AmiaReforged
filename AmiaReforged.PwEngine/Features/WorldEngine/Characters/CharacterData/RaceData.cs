namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;

/// <summary>
/// Value object representing a character's race.
/// Encapsulates race identity and provides type safety over raw strings.
/// </summary>
public record RaceData(string Name)
{
    /// <summary>
    /// Creates a RaceData from a race name string.
    /// </summary>
    public static RaceData From(string raceName) => new(raceName);

    /// <summary>
    /// Implicit conversion from string to RaceData for convenience.
    /// </summary>
    public static implicit operator RaceData(string raceName) => new(raceName);

    /// <summary>
    /// Returns the race name.
    /// </summary>
    public override string ToString() => Name;
}
