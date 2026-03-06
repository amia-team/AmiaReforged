namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Direction of a pin on a Glyph node.
/// </summary>
public enum GlyphPinDirection
{
    /// <summary>
    /// Receives data or execution flow from another node.
    /// </summary>
    Input,

    /// <summary>
    /// Sends data or execution flow to another node.
    /// </summary>
    Output
}
