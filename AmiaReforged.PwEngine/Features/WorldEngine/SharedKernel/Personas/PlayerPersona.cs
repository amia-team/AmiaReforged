using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents a player account persona rooted at the CD key identity.
/// </summary>
public sealed record PlayerPersona : Persona
{
    /// <summary>
    /// Normalized CD key backing this player persona.
    /// </summary>
    public required string CdKey { get; init; }

    /// <summary>
    /// When the persona record was first created.
    /// </summary>
    public required DateTime CreatedUtc { get; init; }

    /// <summary>
    /// When the persona record was last updated.
    /// </summary>
    public required DateTime UpdatedUtc { get; init; }

    /// <summary>
    /// Last time the player was observed in-game, if known.
    /// </summary>
    public DateTime? LastSeenUtc { get; init; }

    /// <summary>
    /// Creates a new player persona record for the supplied CD key.
    /// </summary>
    public static PlayerPersona Create(
        string cdKey,
        string displayName,
        DateTime? createdUtc = null,
        DateTime? updatedUtc = null,
        DateTime? lastSeenUtc = null)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);
        DateTime created = createdUtc ?? DateTime.UtcNow;
        DateTime updated = updatedUtc ?? created;

        PlayerPersona persona = new PlayerPersona
        {
            Id = PersonaId.FromPlayerCdKey(normalizedCdKey),
            Type = PersonaType.Player,
            DisplayName = displayName,
            CdKey = normalizedCdKey,
            CreatedUtc = created,
            UpdatedUtc = updated,
            LastSeenUtc = lastSeenUtc
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}
