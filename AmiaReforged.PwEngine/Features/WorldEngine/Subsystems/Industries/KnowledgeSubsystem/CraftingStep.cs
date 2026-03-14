namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Identifies which aspect of a crafting recipe is being modified by a <see cref="CraftingModifier"/>.
/// Analogous to <see cref="Harvesting.HarvestStep"/> for the harvesting subsystem.
/// </summary>
public enum CraftingStep
{
    /// <summary>
    /// Modifies the quality tier of the produced item(s).
    /// </summary>
    Quality,

    /// <summary>
    /// Modifies the quantity of items produced.
    /// </summary>
    Quantity,

    /// <summary>
    /// Modifies the per-product success chance (for random/optional outputs).
    /// </summary>
    SuccessChance,

    /// <summary>
    /// Modifies the number of rounds needed to complete crafting.
    /// </summary>
    CraftingTime
}
