namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// The type system for Glyph visual scripting pins.
/// Each data type controls what pins can connect and how values are serialized.
/// </summary>
public enum GlyphDataType
{
    /// <summary>
    /// Execution flow pin — controls the order nodes execute.
    /// Rendered as a white diamond. Only connects to other Exec pins.
    /// </summary>
    Exec,

    /// <summary>
    /// Boolean value (true/false). Rendered as a red circle.
    /// </summary>
    Bool,

    /// <summary>
    /// 32-bit signed integer. Rendered as a cyan circle.
    /// </summary>
    Int,

    /// <summary>
    /// 64-bit floating-point number. Rendered as a green circle.
    /// </summary>
    Float,

    /// <summary>
    /// Text string. Rendered as a magenta circle.
    /// </summary>
    String,

    /// <summary>
    /// A reference to an NWN game object (creature, placeable, etc.).
    /// Rendered as a blue circle.
    /// </summary>
    NwObject,

    /// <summary>
    /// An NWN location (position + facing + area). Rendered as a gold circle.
    /// </summary>
    Location,

    /// <summary>
    /// An NWN effect to apply to a creature. Rendered as a purple circle.
    /// </summary>
    Effect,

    /// <summary>
    /// A list of values. The element type is inferred from connected pins.
    /// Rendered as an orange circle.
    /// </summary>
    List
}
