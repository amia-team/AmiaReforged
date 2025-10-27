using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a unique government identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
[JsonConverter(typeof(GovernmentIdJsonConverter))]
public readonly record struct GovernmentId
{
    public Guid Value { get; }

    private GovernmentId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new GovernmentId with a randomly generated GUID.
    /// </summary>
    public static GovernmentId New() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a GovernmentId from an existing GUID value.
    /// </summary>
    /// <param name="id">The GUID to wrap</param>
    /// <returns>A new GovernmentId</returns>
    /// <exception cref="ArgumentException">Thrown when id is Guid.Empty</exception>
    public static GovernmentId From(Guid id) =>
        id == Guid.Empty
            ? throw new ArgumentException("GovernmentId cannot be empty", nameof(id))
            : new(id);

    /// <summary>
    /// Implicit conversion from GovernmentId to Guid for backward compatibility.
    /// </summary>
    public static implicit operator Guid(GovernmentId governmentId) => governmentId.Value;

    /// <summary>
    /// Explicit conversion from Guid to GovernmentId (requires validation).
    /// </summary>
    public static explicit operator GovernmentId(Guid id) => From(id);

    /// <summary>
    /// Converts this GovernmentId to a PersonaId for cross-subsystem references.
    /// </summary>
    public Personas.PersonaId ToPersonaId() => Personas.PersonaId.FromGovernment(this);

    public override string ToString() => Value.ToString();
}

/// <summary>
/// JSON converter for GovernmentId value object.
/// </summary>
public class GovernmentIdJsonConverter : JsonConverter<GovernmentId>
{
    public override GovernmentId Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType == System.Text.Json.JsonTokenType.String)
        {
            string? value = reader.GetString();
            if (value != null && Guid.TryParse(value, out var guid))
            {
                return GovernmentId.From(guid);
            }
        }
        throw new System.Text.Json.JsonException("Invalid GovernmentId format");
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, GovernmentId value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value.ToString());
    }
}

