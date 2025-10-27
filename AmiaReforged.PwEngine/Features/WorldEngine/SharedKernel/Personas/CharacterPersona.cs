namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents a player character as a persona/actor in the world system.
/// </summary>
public sealed record CharacterPersona : Persona
{
    /// <summary>
    /// The strongly-typed character ID (the "real" ID for character-specific operations).
    /// </summary>
    public required CharacterId CharacterId { get; init; }

    /// <summary>
    /// Creates a new CharacterPersona from a CharacterId and display name.
    /// </summary>
    public static CharacterPersona Create(CharacterId characterId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        var persona = new CharacterPersona
        {
            Id = PersonaId.FromCharacter(characterId),
            Type = PersonaType.Character,
            DisplayName = displayName,
            CharacterId = characterId
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}

