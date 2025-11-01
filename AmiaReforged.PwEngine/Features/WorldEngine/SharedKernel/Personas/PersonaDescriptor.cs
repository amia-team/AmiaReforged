using System;
using System.Collections.Generic;
using System.Linq;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Lightweight projection that describes a persona and optional ownership metadata needed by UI layers.
/// </summary>
public sealed record PersonaDescriptor
{
    private static readonly IReadOnlyList<string> EmptyOwners = Array.Empty<string>();

    public PersonaDescriptor(
        PersonaId id,
        PersonaType type,
        string displayName,
        CharacterId? characterId,
        IReadOnlyList<string> ownerCdKeys)
    {
        Id = id;
        Type = type;
        DisplayName = displayName;
        CharacterId = characterId;
        OwnerCdKeys = ownerCdKeys;
    }

    /// <summary>
    /// The unified identifier for the persona.
    /// </summary>
    public PersonaId Id { get; }

    /// <summary>
    /// The persona classification.
    /// </summary>
    public PersonaType Type { get; }

    /// <summary>
    /// Display name resolved for the persona.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Character identifier when the persona represents a player character.
    /// </summary>
    public CharacterId? CharacterId { get; }

    /// <summary>
    /// Ordered set of CD keys that currently own or control the persona.
    /// The first entry represents the primary owner when applicable.
    /// </summary>
    public IReadOnlyList<string> OwnerCdKeys { get; }

    /// <summary>
    /// Convenience accessor for the primary CD key, or null when no ownership is known.
    /// </summary>
    public string? PrimaryOwnerCdKey => OwnerCdKeys.Count > 0 ? OwnerCdKeys[0] : null;

    /// <summary>
    /// Indicates whether the descriptor has any player ownership metadata.
    /// </summary>
    public bool HasKnownOwner => PrimaryOwnerCdKey != null;

    /// <summary>
    /// Factory helper that builds a descriptor from a resolved persona.
    /// </summary>
    public static PersonaDescriptor FromPersona(Persona persona, IEnumerable<string>? ownerCdKeys = null)
    {
        if (persona == null)
            throw new ArgumentNullException(nameof(persona));

        IReadOnlyList<string> ownership = ownerCdKeys?.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray() ?? EmptyOwners;

        CharacterId? characterId = persona is CharacterPersona characterPersona
            ? characterPersona.CharacterId
            : null;

        return new PersonaDescriptor(persona.Id, persona.Type, persona.DisplayName, characterId, ownership);
    }
}
