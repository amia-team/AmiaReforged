using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Provides deterministic account identifiers for personas when provisioning coinhouse accounts.
/// </summary>
public static class PersonaAccountId
{
    private const string Scope = "CoinhouseAccount";

    /// <summary>
    /// Computes a deterministic account identifier for the supplied persona.
    /// Characters and organizations reuse their native GUIDs, all other persona types are hashed.
    /// </summary>
    public static Guid From(PersonaId personaId)
    {
        if (Guid.TryParse(personaId.Value, out Guid parsed))
        {
            return parsed;
        }

        return DeterministicGuidFactory.Create(Scope, personaId.ToString());
    }

    /// <summary>
    /// Computes a deterministic account identifier scoped to a specific coinhouse.
    /// Ensures personas can hold a dedicated account per coinhouse instead of a global account.
    /// </summary>
    public static Guid ForCoinhouse(PersonaId personaId, CoinhouseTag coinhouse)
    {
        string coinhouseKey = coinhouse.Value ?? throw new ArgumentException("Coinhouse tag must have a value", nameof(coinhouse));
        string composite = $"{personaId}:{coinhouseKey.ToLowerInvariant()}";
        return DeterministicGuidFactory.Create(Scope, composite);
    }
}
