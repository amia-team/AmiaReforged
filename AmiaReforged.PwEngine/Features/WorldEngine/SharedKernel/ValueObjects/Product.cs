namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a product created by a recipe
/// </summary>
public record Product
{
    /// <summary>
    /// Item tag (domain identifier from the Item subsystem, e.g., "iron_sword")
    /// </summary>
    public required string ItemTag { get; init; }

    /// <summary>
    /// How many of this item are produced
    /// </summary>
    public required Quantity Quantity { get; init; }

    /// <summary>
    /// Quality/tier of the produced item (nullable - may be determined by process)
    /// </summary>
    public int? Quality { get; init; }

    /// <summary>
    /// Optional: Chance this product is created (for random outputs, 0.0-1.0)
    /// </summary>
    public float? SuccessChance { get; init; }
}

