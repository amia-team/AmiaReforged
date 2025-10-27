namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a unique character identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct CharacterId(Guid Value)
{
    /// <summary>
    /// Creates a new CharacterId with a randomly generated GUID.
    /// </summary>
    public static CharacterId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a CharacterId from an existing GUID value.
    /// </summary>
    /// <param name="id">The GUID to wrap</param>
    /// <returns>A new CharacterId</returns>
    /// <exception cref="ArgumentException">Thrown when id is Guid.Empty</exception>
    public static CharacterId From(Guid id) =>
        id == Guid.Empty
            ? throw new ArgumentException("CharacterId cannot be empty", nameof(id))
            : new(id);

    /// <summary>
    /// Implicit conversion from CharacterId to Guid for backward compatibility.
    /// </summary>
    public static implicit operator Guid(CharacterId characterId) => characterId.Value;

    /// <summary>
    /// Explicit conversion from Guid to CharacterId (requires validation).
    /// </summary>
    public static explicit operator CharacterId(Guid id) => From(id);

    /// <summary>
    /// Converts this CharacterId to a PersonaId for cross-subsystem references.
    /// </summary>
    public Personas.PersonaId ToPersonaId() => Personas.PersonaId.FromCharacter(this);

    public override string ToString() => Value.ToString();
}
