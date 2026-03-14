namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Defines the targeting scope for a <see cref="CraftingModifier"/>.
/// Determines how <see cref="CraftingModifier.TargetTag"/> is interpreted.
/// </summary>
public enum CraftingModifierScope
{
    /// <summary>
    /// Applies to a specific recipe (TargetTag = recipe ID).
    /// </summary>
    Recipe,

    /// <summary>
    /// Applies to all recipes within an industry (TargetTag = industry tag).
    /// </summary>
    Industry,

    /// <summary>
    /// Applies to all recipes globally (TargetTag = "*").
    /// </summary>
    Global
}
