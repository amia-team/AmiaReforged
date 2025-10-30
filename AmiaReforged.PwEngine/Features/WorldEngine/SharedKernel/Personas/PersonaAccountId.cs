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
}
