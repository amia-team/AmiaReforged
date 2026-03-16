using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Describes a tool requirement for a recipe or recipe template. Supports multiple
/// matching modes:
/// <list type="bullet">
///   <item><b>Exact match</b> — set <see cref="ExactItemTag"/> to match a single specific item.</item>
///   <item><b>Form match</b> — set <see cref="RequiredForm"/> to match any item of that tool form
///         (e.g., any <c>ToolFroe</c>).</item>
///   <item><b>Form + Material</b> — set both <see cref="RequiredForm"/> and <see cref="RequiredMaterial"/>
///         to match items of a specific form made from a specific material
///         (e.g., a <c>ToolPick</c> made of <c>Steel</c>).</item>
/// </list>
/// When <see cref="ExactItemTag"/> is provided, form and material constraints are ignored.
/// <see cref="MinQuality"/> may be combined with any mode.
/// </summary>
public record ToolRequirement
{
    /// <summary>
    /// The specific tool form required (e.g., <c>ToolPick</c>, <c>ToolHammer</c>).
    /// Must be a tool-group <see cref="ItemForm"/>. Ignored when <see cref="ExactItemTag"/> is set.
    /// </summary>
    public ItemForm RequiredForm { get; init; }

    /// <summary>
    /// Optional: the specific material the tool must be made of (e.g., <c>Steel</c>, <c>Iron</c>).
    /// When null, any material is acceptable. Ignored when <see cref="ExactItemTag"/> is set.
    /// </summary>
    public MaterialEnum? RequiredMaterial { get; init; }

    /// <summary>
    /// Optional: minimum quality tier the tool must meet.
    /// </summary>
    public int? MinQuality { get; init; }

    /// <summary>
    /// Optional: when set, the requirement matches only the item with this exact tag.
    /// <see cref="RequiredForm"/> and <see cref="RequiredMaterial"/> are ignored in this mode.
    /// </summary>
    public string? ExactItemTag { get; init; }

    /// <summary>
    /// Tests whether the given item blueprint satisfies this tool requirement.
    /// </summary>
    public bool Matches(ItemBlueprint item)
    {
        // Exact tag mode — simple string match
        if (!string.IsNullOrEmpty(ExactItemTag))
        {
            return string.Equals(item.ItemTag, ExactItemTag, StringComparison.OrdinalIgnoreCase);
        }

        // Form must match
        if (item.ItemForm != RequiredForm)
        {
            return false;
        }

        // Material constraint (if specified)
        if (RequiredMaterial.HasValue)
        {
            if (!item.Materials.Contains(RequiredMaterial.Value))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a human-readable description of this requirement.
    /// </summary>
    public string Describe()
    {
        if (!string.IsNullOrEmpty(ExactItemTag))
        {
            return MinQuality.HasValue
                ? $"Exact item '{ExactItemTag}' (min quality {MinQuality})"
                : $"Exact item '{ExactItemTag}'";
        }

        string desc = RequiredMaterial.HasValue
            ? $"{RequiredMaterial.Value} {RequiredForm}"
            : $"Any {RequiredForm}";

        if (MinQuality.HasValue)
        {
            desc += $" (min quality {MinQuality})";
        }

        return desc;
    }
}
