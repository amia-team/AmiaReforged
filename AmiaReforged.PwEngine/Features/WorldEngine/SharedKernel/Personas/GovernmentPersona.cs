using ArgumentException = System.ArgumentException;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents a settlement government as a persona/actor in the world system.
/// </summary>
public sealed record GovernmentPersona : Persona
{
    /// <summary>
    /// The strongly-typed government ID.
    /// </summary>
    public required GovernmentId GovernmentId { get; init; }

    /// <summary>
    /// The settlement this government administers.
    /// </summary>
    public required SettlementId Settlement { get; init; }

    /// <summary>
    /// Creates a new GovernmentPersona from a GovernmentId, settlement, and display name.
    /// </summary>
    public static GovernmentPersona Create(GovernmentId governmentId, SettlementId settlement, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        var persona = new GovernmentPersona
        {
            Id = PersonaId.FromGovernment(governmentId),
            Type = PersonaType.Government,
            DisplayName = displayName,
            GovernmentId = governmentId,
            Settlement = settlement
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}

