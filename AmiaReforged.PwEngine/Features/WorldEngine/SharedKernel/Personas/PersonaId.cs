using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Unified identifier for actors across all subsystems.
/// Format: "{Type}:{UnderlyingId}" (e.g., "Character:550e8400-e29b-41d4-a716-446655440000")
/// This allows subsystems to reference actors uniformly without knowing their concrete type.
/// </summary>
[JsonConverter(typeof(PersonaIdJsonConverter))]
public readonly record struct PersonaId
{
    public PersonaType Type { get; }
    public string Value { get; }

    public PersonaId(PersonaType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PersonaId value cannot be empty", nameof(value));

        Type = type;
        Value = value;
    }

    /// <summary>
    /// Creates a PersonaId from a character ID.
    /// </summary>
    public static PersonaId FromCharacter(CharacterId characterId) =>
        new(PersonaType.Character, characterId.Value.ToString());

    /// <summary>
    /// Creates a PersonaId from an organization ID.
    /// </summary>
    public static PersonaId FromOrganization(OrganizationId orgId) =>
        new(PersonaType.Organization, orgId.Value.ToString());

    /// <summary>
    /// Creates a PersonaId from a coinhouse tag.
    /// </summary>
    public static PersonaId FromCoinhouse(ValueObjects.CoinhouseTag tag) =>
        new(PersonaType.Coinhouse, tag.Value);

    /// <summary>
    /// Creates a PersonaId from a government ID.
    /// </summary>
    public static PersonaId FromGovernment(GovernmentId govId) =>
        new(PersonaType.Government, govId.Value.ToString());

    /// <summary>
    /// Creates a PersonaId for a system process.
    /// </summary>
    public static PersonaId FromSystem(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("System process name cannot be empty", nameof(processName));

        return new(PersonaType.SystemProcess, processName);
    }

    /// <summary>
    /// Parses a PersonaId from its string representation.
    /// Format: "{Type}:{Value}"
    /// </summary>
    public static PersonaId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PersonaId string cannot be empty", nameof(value));

        var parts = value.Split(':', 2);
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid PersonaId format: {value}. Expected 'Type:Value'", nameof(value));

        if (!Enum.TryParse<PersonaType>(parts[0], true, out var type))
            throw new ArgumentException($"Invalid PersonaType: {parts[0]}", nameof(value));

        return new PersonaId(type, parts[1]);
    }

    /// <summary>
    /// Returns the string representation: "{Type}:{Value}"
    /// </summary>
    public override string ToString() => $"{Type}:{Value}";

    /// <summary>
    /// Implicit conversion to string for database storage.
    /// </summary>
    public static implicit operator string(PersonaId personaId) => personaId.ToString();
}

