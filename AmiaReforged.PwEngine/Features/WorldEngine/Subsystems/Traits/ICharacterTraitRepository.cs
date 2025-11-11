using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Repository for persisting character trait selections.
/// </summary>
public interface ICharacterTraitRepository
{
    /// <summary>
    /// Retrieves all traits selected by a character.
    /// </summary>
    /// <param name="characterId">ID of the character</param>
    /// <returns>List of all character traits</returns>
    List<CharacterTrait> GetByCharacterId(CharacterId characterId);

    /// <summary>
    /// Adds a new character trait selection to persistence.
    /// </summary>
    /// <param name="trait">The trait selection to add</param>
    void Add(CharacterTrait trait);

    /// <summary>
    /// Updates an existing character trait (typically confirmation status or custom data).
    /// </summary>
    /// <param name="trait">The trait with updated values</param>
    void Update(CharacterTrait trait);

    /// <summary>
    /// Removes a character trait selection from persistence.
    /// </summary>
    /// <param name="traitId">ID of the trait selection to delete</param>
    void Delete(Guid traitId);
}
