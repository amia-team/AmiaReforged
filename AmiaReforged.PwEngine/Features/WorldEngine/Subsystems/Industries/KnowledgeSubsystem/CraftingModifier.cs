namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Defines a modifier that a piece of <see cref="Knowledge"/> applies to crafting recipes.
/// Analogous to <see cref="KnowledgeHarvestEffect"/> for the harvesting subsystem.
/// <para>
/// <see cref="TargetTag"/> and <see cref="Scope"/> determine which recipes this modifier
/// applies to. <see cref="StepModified"/> identifies the crafting attribute being changed,
/// and <see cref="Value"/> + <see cref="Operation"/> describe the numeric modification.
/// </para>
/// </summary>
/// <param name="TargetTag">
/// The target identifier — interpretation depends on <see cref="Scope"/>:
/// <list type="bullet">
///   <item><description><see cref="CraftingModifierScope.Recipe"/> → recipe ID string</description></item>
///   <item><description><see cref="CraftingModifierScope.Industry"/> → industry tag</description></item>
///   <item><description><see cref="CraftingModifierScope.Global"/> → <c>"*"</c></description></item>
/// </list>
/// </param>
/// <param name="Scope">How <paramref name="TargetTag"/> is matched against recipes.</param>
/// <param name="StepModified">Which crafting attribute is modified.</param>
/// <param name="Value">Numeric value of the modification.</param>
/// <param name="Operation">How <paramref name="Value"/> is applied (additive, percent, subtractive).</param>
public record CraftingModifier(
    string TargetTag,
    CraftingModifierScope Scope,
    CraftingStep StepModified,
    float Value,
    EffectOperation Operation)
{
    /// <summary>
    /// Returns <c>true</c> if this modifier applies to the given recipe/industry combination.
    /// </summary>
    public bool Matches(string recipeId, string industryTag) => Scope switch
    {
        CraftingModifierScope.Recipe => string.Equals(TargetTag, recipeId, System.StringComparison.OrdinalIgnoreCase),
        CraftingModifierScope.Industry => string.Equals(TargetTag, industryTag, System.StringComparison.OrdinalIgnoreCase),
        CraftingModifierScope.Global => true,
        _ => false
    };
}
