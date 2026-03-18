namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Defines a configurable property on a Glyph node. Unlike pins, properties are not
/// connected to other nodes — they are static configuration values set in the editor
/// and stored in <see cref="GlyphNodeInstance.PropertyOverrides"/>.
/// <para>
/// When <see cref="AllowedValues"/> is non-empty, the editor renders a dropdown instead
/// of a free-text input.
/// </para>
/// </summary>
public record GlyphPropertyDefinition
{
    /// <summary>
    /// Unique identifier for this property within its parent node definition.
    /// This key is used in <see cref="GlyphNodeInstance.PropertyOverrides"/>.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable label shown in the editor property panel.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Default value when no override has been set. Also the initial selection in dropdowns.
    /// </summary>
    public string DefaultValue { get; init; } = string.Empty;

    /// <summary>
    /// When non-empty, restricts the property to these values and the editor renders a dropdown.
    /// When empty, a free-text input is shown.
    /// </summary>
    public List<string> AllowedValues { get; init; } = [];
}
