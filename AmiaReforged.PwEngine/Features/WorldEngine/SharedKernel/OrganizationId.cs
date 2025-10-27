using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a unique organization identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
[JsonConverter(typeof(OrganizationIdJsonConverter))]
public readonly record struct OrganizationId
{
    public Guid Value { get; }

    private OrganizationId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new OrganizationId with a randomly generated GUID.
    /// </summary>
    public static OrganizationId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates an OrganizationId from an existing GUID value.
    /// </summary>
    /// <param name="id">The GUID to wrap</param>
    /// <returns>A new OrganizationId</returns>
    /// <exception cref="ArgumentException">Thrown when id is Guid.Empty</exception>
    public static OrganizationId From(Guid id) =>
        id == Guid.Empty
            ? throw new ArgumentException("OrganizationId cannot be empty", nameof(id))
            : new(id);

    /// <summary>
    /// Implicit conversion from OrganizationId to Guid for backward compatibility.
    /// </summary>
    public static implicit operator Guid(OrganizationId organizationId) => organizationId.Value;

    /// <summary>
    /// Explicit conversion from Guid to OrganizationId (requires validation).
    /// </summary>
    public static explicit operator OrganizationId(Guid id) => From(id);

    /// <summary>
    /// Converts this OrganizationId to a PersonaId for cross-subsystem references.
    /// </summary>
    public Personas.PersonaId ToPersonaId() => Personas.PersonaId.FromOrganization(this);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// JSON converter for OrganizationId value object.
/// </summary>
public class OrganizationIdJsonConverter : JsonConverter<OrganizationId>
{
    public override OrganizationId Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value != null && Guid.TryParse(value, out Guid guid))
            {
                return OrganizationId.From(guid);
            }
        }
        throw new System.Text.Json.JsonException("Invalid OrganizationId format");
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, OrganizationId value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

