using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

/// <summary>
/// Extension methods for display names on <see cref="MaterialEnum"/> values.
/// Mirrors the <see cref="Harvesting.ItemFormExtensions"/> pattern with a
/// <see cref="FrozenDictionary{TKey,TValue}"/> built once at startup.
/// </summary>
public static partial class MaterialEnumExtensions
{
    private static readonly FrozenDictionary<MaterialEnum, string> DisplayNameMap = BuildDisplayNameMap();

    /// <summary>
    /// Returns a human-friendly display name by stripping the category prefix, inserting
    /// spaces before each capital letter, and reattaching a suffix where appropriate.
    /// For example, <c>HideDragonBlack</c> → <c>"Black Dragon Hide"</c>,
    /// <c>HideSalamander</c> → <c>"Salamander Hide"</c>,
    /// <c>WoodOak</c> → <c>"Oak"</c>, <c>ColdIron</c> → <c>"Cold Iron"</c>.
    /// </summary>
    public static string DisplayName(this MaterialEnum material)
    {
        return DisplayNameMap.GetValueOrDefault(material, material.ToString());
    }

    private static FrozenDictionary<MaterialEnum, string> BuildDisplayNameMap()
    {
        Dictionary<MaterialEnum, string> dict = new();

        // Prefix → suffix to reattach after stripping (longest first so "HideDragon" matches before "Hide")
        (string Prefix, string Suffix)[] prefixRules =
        [
            ("HideDragon", " Dragon Hide"),
            ("GemCrystal", " Crystal"),
            ("Elemental", ""),
            ("Hide", " Hide"),
            ("Wood", ""),
            ("Gem", ""),
        ];

        foreach (MaterialEnum value in Enum.GetValues<MaterialEnum>())
        {
            if (value is MaterialEnum.None or MaterialEnum.Unknown) continue;

            string raw = value.ToString();

            // Try to match a prefix rule
            string stripped = raw;
            string suffix = "";
            foreach ((string prefix, string sfx) in prefixRules)
            {
                if (raw.StartsWith(prefix, StringComparison.Ordinal) && raw.Length > prefix.Length)
                {
                    stripped = raw[prefix.Length..];
                    suffix = sfx;
                    break;
                }
            }

            // Insert spaces before uppercase letters: "ColdIron" → "Cold Iron"
            string displayName = PascalSplitRegex().Replace(stripped, " $1").Trim() + suffix;
            dict[value] = displayName;
        }

        return dict.ToFrozenDictionary();
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])")]
    private static partial Regex PascalSplitRegex();
}
