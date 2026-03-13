using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a product output in a recipe template. The output item is resolved dynamically
/// by matching the <see cref="OutputForm"/> and the material inherited from the ingredient
/// at <see cref="MaterialSourceSlot"/>.
/// </summary>
public record TemplateProduct
{
    /// <summary>
    /// The item form for the output (e.g., <c>ResourcePlank</c>, <c>ResourceIngot</c>).
    /// Matches against <see cref="Items.ItemData.ItemBlueprint.JobSystemType"/>.
    /// </summary>
    public required JobSystemItemType OutputForm { get; init; }

    /// <summary>
    /// The index of the <see cref="TemplateIngredient"/> whose resolved material
    /// determines the output material. For example, if slot 0 resolves to Oak (Wood),
    /// the product will be the item with form <see cref="OutputForm"/> and material Oak.
    /// </summary>
    public required int MaterialSourceSlot { get; init; }

    /// <summary>
    /// How many of this item are produced.
    /// </summary>
    public required Quantity Quantity { get; init; }

    /// <summary>
    /// Optional fixed quality for the output item.
    /// </summary>
    public int? Quality { get; init; }

    /// <summary>
    /// Optional: Chance this product is created (for random outputs, 0.0–1.0).
    /// </summary>
    public float? SuccessChance { get; init; }
}
