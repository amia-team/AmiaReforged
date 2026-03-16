using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Result of a crafting operation
/// </summary>
public class CraftingResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Product> ProductsCreated { get; init; } = [];
    public List<Ingredient> IngredientsConsumed { get; init; } = [];
    public int ProgressionPointsAwarded { get; init; }
    public CraftingFailureReason? FailureReason { get; init; }
}

/// <summary>
/// Reasons why crafting might fail
/// </summary>
public enum CraftingFailureReason
{
    InsufficientKnowledge,
    MissingIngredients,
    InsufficientQuality,
    ProcessFailed, // Industry-specific failure
    Unknown
}

