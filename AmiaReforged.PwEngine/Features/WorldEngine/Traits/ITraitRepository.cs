namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// In-memory registry of trait definitions loaded from JSON at startup.
/// </summary>
public interface ITraitRepository
{
    /// <summary>
    /// Checks if a trait definition exists.
    /// </summary>
    /// <param name="traitTag">Unique tag of the trait</param>
    /// <returns>True if trait exists, false otherwise</returns>
    bool TraitExists(string traitTag);

    /// <summary>
    /// Adds a trait definition to the registry.
    /// </summary>
    /// <param name="trait">The trait to add</param>
    void Add(Trait trait);

    /// <summary>
    /// Retrieves a trait definition by tag.
    /// </summary>
    /// <param name="traitTag">Unique tag of the trait</param>
    /// <returns>The trait if found, null otherwise</returns>
    Trait? Get(string traitTag);

    /// <summary>
    /// Retrieves all trait definitions.
    /// </summary>
    /// <returns>List of all registered traits</returns>
    List<Trait> All();
}
