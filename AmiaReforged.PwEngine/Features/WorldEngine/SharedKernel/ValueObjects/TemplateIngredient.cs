using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a template ingredient that matches items by material category and form
/// rather than by a specific item tag. Used in <see cref="Industries.RecipeTemplate"/>.
/// </summary>
public record TemplateIngredient
{
    /// <summary>
    /// The material family required (e.g., <c>Wood</c>, <c>Metal</c>).
    /// Any item whose <see cref="MaterialEnum"/> belongs to this category is a valid match.
    /// </summary>
    public required MaterialCategory RequiredCategory { get; init; }

    /// <summary>
    /// The item form required (e.g., <c>ResourceLog</c>, <c>ResourceIngot</c>).
    /// Matches against <see cref="ItemBlueprint.ItemForm"/>.
    /// </summary>
    public required ItemForm RequiredForm { get; init; }

    /// <summary>
    /// How many of this item are needed.
    /// </summary>
    public required Quantity Quantity { get; init; }

    /// <summary>
    /// Minimum quality tier required (nullable — some templates don't care about quality).
    /// </summary>
    public int? MinQuality { get; init; }

    /// <summary>
    /// Whether the ingredient is consumed during crafting, or acts as a tool/catalyst.
    /// </summary>
    public bool IsConsumed { get; init; } = true;

    /// <summary>
    /// Positional index for this ingredient slot. Used by <see cref="TemplateProduct.MaterialSourceSlot"/>
    /// to determine which ingredient's resolved material defines the output product's material.
    /// </summary>
    public required int SlotIndex { get; init; }
}
