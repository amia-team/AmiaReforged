using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;

public interface ILocalVariableSanitizationService
{
    /// <summary>
    /// Registers a sanitizer with the service. Registered sanitizers execute for every player during login sanitization.
    /// </summary>
    /// <param name="sanitizer">The sanitizer instance to register.</param>
    void RegisterSanitizer(ILocalVariableSanitizer sanitizer);

    /// <summary>
    /// Executes all registered sanitizers for the supplied player.
    /// </summary>
    /// <param name="player">The player whose associated creatures should be sanitized.</param>
    void Sanitize(NwPlayer player);

    /// <summary>
    /// Executes all registered sanitizers against a specific creature.
    /// </summary>
    /// <param name="creature">The creature to sanitize.</param>
    void Sanitize(NwCreature creature);
}
