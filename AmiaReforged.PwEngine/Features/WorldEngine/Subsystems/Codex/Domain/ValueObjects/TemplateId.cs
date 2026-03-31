namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique dynamic quest template identifier.
/// Templates are blueprints from which dynamic quest postings are instantiated.
/// </summary>
public readonly record struct TemplateId
{
    public Guid Value { get; }

    public TemplateId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TemplateId cannot be an empty GUID", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new TemplateId with a unique GUID-based identifier.
    /// </summary>
    public static TemplateId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Implicit conversion from TemplateId to string for convenience.
    /// </summary>
    public static implicit operator string(TemplateId templateId) => templateId.Value.ToString();

    /// <summary>
    /// Explicit conversion from Guid to TemplateId (requires validation).
    /// </summary>
    public static explicit operator TemplateId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
