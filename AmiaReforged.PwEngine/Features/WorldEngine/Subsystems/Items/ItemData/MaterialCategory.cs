using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

/// <summary>
/// Broad material families used by recipe templates to match any material within a category.
/// For example, <c>MaterialCategory.Wood</c> matches Oak, Pine, Cedar, Ironwood, etc.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaterialCategory
{
    None = 0,
    Metal = 1,
    Wood = 2,
    Creature = 3,
    Plant = 4,
    Gem = 6,
    Stone = 7,
    Elemental = 8,
}

/// <summary>
/// Attribute applied to <see cref="MaterialEnum"/> members to declare their <see cref="MaterialCategory"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class MaterialCategoryAttribute : Attribute
{
    public MaterialCategory Category { get; }

    public MaterialCategoryAttribute(MaterialCategory category)
    {
        Category = category;
    }
}

/// <summary>
/// Extension methods for querying material categories on <see cref="MaterialEnum"/> values.
/// </summary>
public static class MaterialCategoryExtensions
{
    private static readonly FrozenDictionary<MaterialEnum, MaterialCategory> CategoryMap = BuildCategoryMap();

    /// <summary>
    /// Returns the <see cref="MaterialCategory"/> for this material, or <c>MaterialCategory.None</c>
    /// if no <see cref="MaterialCategoryAttribute"/> is declared.
    /// </summary>
    public static MaterialCategory GetCategory(this MaterialEnum material)
    {
        return CategoryMap.GetValueOrDefault(material, MaterialCategory.None);
    }

    /// <summary>
    /// Returns all <see cref="MaterialEnum"/> values that belong to the given category.
    /// </summary>
    public static IReadOnlyList<MaterialEnum> GetMaterialsInCategory(MaterialCategory category)
    {
        return CategoryMap
            .Where(kvp => kvp.Value == category)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private static FrozenDictionary<MaterialEnum, MaterialCategory> BuildCategoryMap()
    {
        Dictionary<MaterialEnum, MaterialCategory> dict = new Dictionary<MaterialEnum, MaterialCategory>();

        foreach (MaterialEnum value in Enum.GetValues<MaterialEnum>())
        {
            FieldInfo? memberInfo = typeof(MaterialEnum).GetField(value.ToString());
            MaterialCategoryAttribute? attr = memberInfo?.GetCustomAttribute<MaterialCategoryAttribute>();
            if (attr != null)
            {
                dict[value] = attr.Category;
            }
        }

        return dict.ToFrozenDictionary();
    }
}
