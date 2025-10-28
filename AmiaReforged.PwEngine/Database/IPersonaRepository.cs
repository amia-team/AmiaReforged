using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
/// Repository for looking up personas across different entity types.
/// Provides unified access to any actor (character, organization, coinhouse, etc.) via PersonaId.
/// </summary>
public interface IPersonaRepository
{
    /// <summary>
    /// Attempts to resolve a PersonaId to its concrete Persona.
    /// </summary>
    /// <param name="personaId">The PersonaId to resolve</param>
    /// <param name="persona">The resolved Persona, or null if not found</param>
    /// <returns>True if persona was found and resolved, false otherwise</returns>
    bool TryGetPersona(PersonaId personaId, out Persona? persona);

    /// <summary>
    /// Gets a Persona by PersonaId, throwing if not found.
    /// </summary>
    /// <param name="personaId">The PersonaId to resolve</param>
    /// <returns>The resolved Persona</returns>
    /// <exception cref="InvalidOperationException">Thrown when persona is not found</exception>
    Persona GetPersona(PersonaId personaId);

    /// <summary>
    /// Checks if a PersonaId exists in the system.
    /// </summary>
    /// <param name="personaId">The PersonaId to check</param>
    /// <returns>True if the persona exists, false otherwise</returns>
    bool Exists(PersonaId personaId);

    /// <summary>
    /// Gets the display name for a PersonaId without loading the full entity.
    /// </summary>
    /// <param name="personaId">The PersonaId</param>
    /// <returns>The display name, or null if not found</returns>
    string? GetDisplayName(PersonaId personaId);

    /// <summary>
    /// Resolves multiple PersonaIds in a single query.
    /// More efficient than calling TryGetPersona multiple times.
    /// </summary>
    /// <param name="personaIds">The PersonaIds to resolve</param>
    /// <returns>Dictionary mapping PersonaId to Persona (only includes found personas)</returns>
    Dictionary<PersonaId, Persona> GetPersonas(IEnumerable<PersonaId> personaIds);
}

