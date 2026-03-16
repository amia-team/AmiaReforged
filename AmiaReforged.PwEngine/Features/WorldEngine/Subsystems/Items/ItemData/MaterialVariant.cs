namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

/// <summary>
/// Defines a material variant for an item blueprint template. Each variant produces
/// a concrete <see cref="ItemBlueprint"/> with the specified material and appearance.
/// <para>
/// When an <see cref="ItemBlueprint"/> has a non-empty <see cref="ItemBlueprint.Variants"/>
/// list, the <see cref="ItemBlueprintExpander"/> generates one concrete item per variant,
/// inheriting the template's shared properties (ResRef, ItemForm, BaseItemType, etc.)
/// while substituting the variant's material, appearance, and optional base value.
/// </para>
/// </summary>
public record MaterialVariant
{
    /// <summary>
    /// The material for this variant. Used to derive the expanded item's tag
    /// (<c>{templateTag}_{material}</c>) and name (<c>{Material} {TemplateName}</c>).
    /// </summary>
    public MaterialEnum Material { get; init; }

    /// <summary>
    /// The visual appearance data for this variant. Each material variant will typically
    /// have different model or color settings.
    /// </summary>
    public AppearanceData Appearance { get; init; } = new(0, null, null);

    /// <summary>
    /// Optional override for the item's base value. When null, the template's
    /// <see cref="ItemBlueprint.BaseValue"/> is inherited.
    /// </summary>
    public int? BaseValueOverride { get; init; }
}
