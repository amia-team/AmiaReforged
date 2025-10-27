namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Meta-entity representing any actor in the world.
/// Domain entities have their own strongly-typed IDs AND a PersonaId for cross-subsystem references.
/// </summary>
public abstract record Persona
{
    /// <summary>
    /// The unified identifier for this persona across all subsystems.
    /// </summary>
    public required PersonaId Id { get; init; }

    /// <summary>
    /// The type of persona this represents.
    /// </summary>
    public required PersonaType Type { get; init; }

    /// <summary>
    /// The display name for this persona (used in UIs, logs, etc.).
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Validates that the persona's type matches the PersonaId type.
    /// </summary>
    protected void ValidateTypeConsistency()
    {
        if (Id.Type != Type)
            throw new InvalidOperationException(
                $"Persona type mismatch: Persona.Type is {Type} but PersonaId.Type is {Id.Type}");
    }
}

