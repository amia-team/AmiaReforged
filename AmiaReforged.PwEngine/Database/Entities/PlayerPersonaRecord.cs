using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// Persistence model for player-level personas keyed by NWN CD key.
/// </summary>
public sealed class PlayerPersonaRecord
{
    /// <summary>
    /// Normalized (upper-case) CD key for the player; serves as the primary key.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public required string CdKey { get; set; }

    /// <summary>
    /// Display name to show for the player persona (current in-game player name).
    /// </summary>
    [MaxLength(255)]
    public required string DisplayName { get; set; }

    /// <summary>
    /// Backing string for the persona identifier. Defaults to "Player:{CdKey}" when not set.
    /// </summary>
    [MaxLength(256)]
    public string? PersonaIdString { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public DateTime? LastSeenUtc { get; set; }

    [NotMapped]
    public PersonaId PersonaId
    {
        get
        {
            string value = PersonaIdString ?? PersonaId.FromPlayerCdKey(CdKey).ToString();
            return PersonaId.Parse(value);
        }
    }
}
