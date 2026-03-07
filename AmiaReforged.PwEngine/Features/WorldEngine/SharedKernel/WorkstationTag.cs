namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a workstation identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct WorkstationTag
{
    public string Value { get; }

    public WorkstationTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("WorkstationTag cannot be null or whitespace", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("WorkstationTag cannot exceed 50 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Implicit conversion from WorkstationTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(WorkstationTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to WorkstationTag (requires validation).
    /// </summary>
    public static explicit operator WorkstationTag(string value) => new(value);

    public override string ToString() => Value;
}
