namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique posting identifier for a live dynamic quest instance.
/// Each posting is a claimable instance created from a <see cref="TemplateId"/>.
/// </summary>
public readonly record struct PostingId
{
    public Guid Value { get; }

    public PostingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PostingId cannot be an empty GUID", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new PostingId with a unique GUID-based identifier.
    /// </summary>
    public static PostingId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Implicit conversion from PostingId to string for convenience.
    /// </summary>
    public static implicit operator string(PostingId postingId) => postingId.Value.ToString();

    /// <summary>
    /// Explicit conversion from Guid to PostingId (requires validation).
    /// </summary>
    public static explicit operator PostingId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
