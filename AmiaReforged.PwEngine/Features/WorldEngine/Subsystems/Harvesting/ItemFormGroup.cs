namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

/// <summary>
/// Manufacturing-oriented classification groups for <see cref="ItemForm"/> values.
/// Each group aggregates related item forms for UI grouping, filtering, and business logic.
/// </summary>
public enum ItemFormGroup
{
    /// <summary>No group assigned.</summary>
    None = 0,

    /// <summary>Hand-held implements required to perform harvesting (Pick, Hammer, Axe, etc.).</summary>
    Tool = 1,

    /// <summary>Raw materials gathered directly from resource nodes (Ore, Stone, Log, Gem, Plant).</summary>
    Resource = 2,

    /// <summary>Products created by processing raw resources (Plank, Brick, Ingot).</summary>
    IntermediateProduct = 3,

    /// <summary>End-user goods produced from intermediate products (Furniture, etc.).</summary>
    FinishedGood = 4,

    /// <summary>Discrete parts used as sub-assemblies in more complex recipes (future use).</summary>
    Component = 5,
}
