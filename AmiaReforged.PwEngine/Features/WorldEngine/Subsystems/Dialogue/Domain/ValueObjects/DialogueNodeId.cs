namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique dialogue node identifier within a tree.
/// </summary>
public readonly record struct DialogueNodeId
{
    public Guid Value { get; }

    public DialogueNodeId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("DialogueNodeId cannot be empty", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new DialogueNodeId with a unique GUID.
    /// </summary>
    public static DialogueNodeId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a DialogueNodeId from an existing GUID.
    /// </summary>
    public static DialogueNodeId From(Guid guid) => new(guid);

    /// <summary>
    /// Implicit conversion to Guid.
    /// </summary>
    public static implicit operator Guid(DialogueNodeId id) => id.Value;

    /// <summary>
    /// Explicit conversion from Guid.
    /// </summary>
    public static explicit operator DialogueNodeId(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
