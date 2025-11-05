using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;

/// <summary>
/// Represents a unit of sanitization logic that can migrate or repair legacy local variables on player-controlled creatures.
/// </summary>
public interface ILocalVariableSanitizer
{
    /// <summary>
    /// Gets a human-readable name for the sanitizer, used for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies any required sanitization to the provided creature.
    /// </summary>
    /// <param name="creature">The creature to sanitize. Implementations should assume this is executed on the main thread.</param>
    void Sanitize(NwCreature creature);
}
