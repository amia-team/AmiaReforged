using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Traits;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.TraitSystem;

/// <summary>
/// Test stub implementing ICharacterInfo for trait eligibility testing.
/// Uses strong types for race and classes.
/// </summary>
public class TestCharacterInfo : ICharacterInfo
{
    public required RaceData Race { get; init; }
    public required IReadOnlyList<CharacterClassData> Classes { get; init; }

    /// <summary>
    /// Creates a test character with simple string values (convenience method).
    /// </summary>
    public static TestCharacterInfo From(string race, params string[] classes) =>
        new()
        {
            Race = new RaceData(race),
            Classes = classes.Select(c => CharacterClassData.From(c)).ToList()
        };
}
