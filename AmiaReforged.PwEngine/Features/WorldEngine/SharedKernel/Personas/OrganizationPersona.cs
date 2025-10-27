namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents an organization (guild, faction, company) as a persona/actor in the world system.
/// </summary>
public sealed record OrganizationPersona : Persona
{
    /// <summary>
    /// The strongly-typed organization ID (the "real" ID for organization-specific operations).
    /// </summary>
    public required OrganizationId OrganizationId { get; init; }

    /// <summary>
    /// Creates a new OrganizationPersona from an OrganizationId and display name.
    /// </summary>
    public static OrganizationPersona Create(OrganizationId organizationId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name cannot be empty", nameof(displayName));

        var persona = new OrganizationPersona
        {
            Id = PersonaId.FromOrganization(organizationId),
            Type = PersonaType.Organization,
            DisplayName = displayName,
            OrganizationId = organizationId
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}

