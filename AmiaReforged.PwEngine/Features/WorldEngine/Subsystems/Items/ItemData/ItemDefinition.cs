using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

public record ItemBlueprint(
    string ResRef,
    string ItemTag,
    string Name,
    string Description,
    MaterialEnum[] Materials,
    ItemForm ItemForm,
    int BaseItemType,
    AppearanceData Appearance,
    IReadOnlyList<JsonLocalVariableDefinition>? LocalVariables = null,
    int BaseValue = 1,
    int WeightIncreaseConstant = -1)
{
    /// <summary>
    /// The source filename (without extension) this blueprint was loaded from.
    /// Used as a fallback lookup key when ItemTag doesn't match.
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// Optional list of material variants. When populated, this blueprint acts as a template
    /// and the <see cref="ItemBlueprintExpander"/> generates one concrete <see cref="ItemBlueprint"/>
    /// per variant, each with its own material, appearance, and optionally overridden base value.
    /// </summary>
    public List<MaterialVariant>? Variants { get; init; }

    /// <summary>
    /// True when this blueprint defines material variants and should be expanded.
    /// </summary>
    public bool IsTemplate => Variants is { Count: > 0 };
}
