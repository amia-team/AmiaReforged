using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Interface providing character information needed for trait eligibility checks.
/// Adapters at the NWN boundary will implement this for actual game characters.
/// Uses strong types instead of primitives to prevent bugs and improve clarity.
/// </summary>
public interface ICharacterInfo
{
    /// <summary>
    /// The character's race as a strong type.
    /// </summary>
    RaceData Race { get; }

    /// <summary>
    /// All classes the character has as a read-only collection of strong types.
    /// </summary>
    IReadOnlyList<CharacterClassData> Classes { get; }
}

