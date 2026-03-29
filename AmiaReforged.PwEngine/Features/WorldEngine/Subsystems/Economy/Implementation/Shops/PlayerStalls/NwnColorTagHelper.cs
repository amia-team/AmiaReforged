using System.Text.RegularExpressions;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Strips NWN color tags from strings for display purposes.
/// Color tags follow the pattern &lt;cRGB&gt;text&lt;/c&gt; where the opening tag
/// can contain arbitrary bytes (raw RGB values), spaces, and special characters.
/// </summary>
public static partial class NwnColorTagHelper
{
    // Matches <c followed by any characters up to > (the opening color tag),
    // OR the closing </c> tag. The [^>]* is intentionally permissive to handle
    // the arbitrary bytes NWN encodes as RGB color values.
    [GeneratedRegex(@"<c[^>]*>|</c>")]
    private static partial Regex ColorTagPattern();

    /// <summary>
    /// Removes all NWN color tags from the given string, returning plain text.
    /// The original string is not modified.
    /// </summary>
    public static string StripColorTags(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

        return ColorTagPattern().Replace(input, string.Empty);
    }
}
