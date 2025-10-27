using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents a coinhouse (banking institution) as a persona/actor in the world system.
/// </summary>
public sealed record CoinhousePersona : Persona
{
    /// <summary>
    /// The coinhouse tag identifier.
    /// </summary>
    public required CoinhouseTag Tag { get; init; }

    /// <summary>
    /// The settlement where this coinhouse is located.
    /// </summary>
    public required SettlementId Settlement { get; init; }

    /// <summary>
    /// Creates a new CoinhousePersona from a CoinhouseTag, settlement, and display name.
    /// </summary>
    public static CoinhousePersona Create(CoinhouseTag tag, SettlementId settlement, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        var persona = new CoinhousePersona
        {
            Id = PersonaId.FromCoinhouse(tag),
            Type = PersonaType.Coinhouse,
            DisplayName = displayName,
            Tag = tag,
            Settlement = settlement
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}

