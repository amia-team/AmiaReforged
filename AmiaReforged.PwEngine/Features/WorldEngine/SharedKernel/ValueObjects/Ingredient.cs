namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents an ingredient required for a recipe
/// </summary>
public record Ingredient
{
    /// <summary>
    /// Resource reference for the item (e.g., "iron_ingot")
    /// </summary>
    public required string ItemResRef { get; init; }

    /// <summary>
    /// How many of this item are needed
    /// </summary>
    public required Quantity Quantity { get; init; }

    /// <summary>
    /// Minimum quality/tier required (nullable - some recipes don't care about quality)
    /// </summary>
    public int? MinQuality { get; init; }

    /// <summary>
    /// Optional: Must the ingredient be consumed, or is it a tool/catalyst?
    /// </summary>
    public bool IsConsumed { get; init; } = true;
}

