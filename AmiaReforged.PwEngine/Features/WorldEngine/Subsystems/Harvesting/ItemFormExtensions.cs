using System.Collections.Frozen;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

/// <summary>
/// Extension methods for querying groups and display names on <see cref="ItemForm"/> values.
/// Mirrors the <see cref="Items.ItemData.MaterialCategoryExtensions"/> pattern with a
/// <see cref="FrozenDictionary{TKey,TValue}"/> built once at startup via reflection.
/// </summary>
public static partial class ItemFormExtensions
{
    private static readonly FrozenDictionary<ItemForm, ItemFormGroup> GroupMap = BuildGroupMap();
    private static readonly FrozenDictionary<ItemForm, string> DisplayNameMap = BuildDisplayNameMap();

    // ── Queries ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="ItemFormGroup"/> for this form, or <c>ItemFormGroup.None</c>
    /// if no <see cref="ItemFormGroupAttribute"/> is declared.
    /// </summary>
    public static ItemFormGroup GetGroup(this ItemForm form)
    {
        return GroupMap.GetValueOrDefault(form, ItemFormGroup.None);
    }

    /// <summary>Shorthand: is this form in the <see cref="ItemFormGroup.Tool"/> group?</summary>
    public static bool IsTool(this ItemForm form) => form.GetGroup() == ItemFormGroup.Tool;

    /// <summary>Shorthand: is this form in the <see cref="ItemFormGroup.Resource"/> group?</summary>
    public static bool IsResource(this ItemForm form) => form.GetGroup() == ItemFormGroup.Resource;

    /// <summary>
    /// Returns a human-friendly display name by stripping the group prefix and inserting spaces
    /// before each capital letter. For example, <c>ToolWoodChisel</c> → <c>"Wood Chisel"</c>,
    /// <c>ResourceOre</c> → <c>"Ore"</c>.
    /// </summary>
    public static string DisplayName(this ItemForm form)
    {
        return DisplayNameMap.GetValueOrDefault(form, form.ToString());
    }

    /// <summary>
    /// Returns all <see cref="ItemForm"/> values that belong to the given group.
    /// </summary>
    public static IReadOnlyList<ItemForm> GetFormsInGroup(ItemFormGroup group)
    {
        return GroupMap
            .Where(kvp => kvp.Value == group)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    // ── Builders ─────────────────────────────────────────────────────

    private static FrozenDictionary<ItemForm, ItemFormGroup> BuildGroupMap()
    {
        Dictionary<ItemForm, ItemFormGroup> dict = new();

        foreach (ItemForm value in Enum.GetValues<ItemForm>())
        {
            FieldInfo? memberInfo = typeof(ItemForm).GetField(value.ToString());
            ItemFormGroupAttribute? attr = memberInfo?.GetCustomAttribute<ItemFormGroupAttribute>();
            if (attr != null)
            {
                dict[value] = attr.Group;
            }
        }

        return dict.ToFrozenDictionary();
    }

    private static FrozenDictionary<ItemForm, string> BuildDisplayNameMap()
    {
        Dictionary<ItemForm, string> dict = new();

        // Known prefixes to strip (order matters — longest first)
        string[] prefixes = ["Tool", "Resource"];

        foreach (ItemForm value in Enum.GetValues<ItemForm>())
        {
            if (value == ItemForm.None) continue;

            string raw = value.ToString();

            // Strip group prefix
            string stripped = raw;
            foreach (string prefix in prefixes)
            {
                if (raw.StartsWith(prefix, StringComparison.Ordinal) && raw.Length > prefix.Length)
                {
                    stripped = raw[prefix.Length..];
                    break;
                }
            }

            // Insert spaces before uppercase letters: "WoodChisel" → "Wood Chisel"
            string displayName = PascalSplitRegex().Replace(stripped, " $1").Trim();
            dict[value] = displayName;
        }

        return dict.ToFrozenDictionary();
    }

    [GeneratedRegex(@"(?<=[a-z])([A-Z])")]
    private static partial Regex PascalSplitRegex();
}
