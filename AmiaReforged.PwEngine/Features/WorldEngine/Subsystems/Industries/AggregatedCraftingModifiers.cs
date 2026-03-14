using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// The net result of aggregating all applicable <see cref="CraftingModifier"/>s for a single
/// crafting operation. Each field represents the combined modification to that crafting aspect.
/// <para>
/// Use <see cref="Aggregate"/> to compute this from a list of raw modifiers.
/// Use <see cref="None"/> when no modifiers apply.
/// </para>
/// </summary>
public record AggregatedCraftingModifiers
{
    /// <summary>
    /// Net additive bonus to output item quality tiers.
    /// Applied after base quality is determined from input items.
    /// Clamped to NWN quality range (0–9) at application time.
    /// </summary>
    public int QualityBonus { get; init; }

    /// <summary>
    /// Multiplier applied to output quantity. Defaults to 1.0 (no change).
    /// Result is floored to int, minimum 1.
    /// </summary>
    public float QuantityMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Flat bonus added to per-product success chance.
    /// Clamped to [0.0, 1.0] at application time.
    /// </summary>
    public float SuccessChanceBonus { get; init; }

    /// <summary>
    /// Rounds subtracted from the base crafting time.
    /// The final crafting time is <c>max(1, baseRounds - TimeReductionRounds)</c>.
    /// </summary>
    public int TimeReductionRounds { get; init; }

    /// <summary>
    /// A zero-effect instance — no modifications applied.
    /// </summary>
    public static readonly AggregatedCraftingModifiers None = new();

    /// <summary>
    /// Returns <c>true</c> if all fields are at their default (no-op) values.
    /// </summary>
    public bool IsEmpty => QualityBonus == 0
                           && Math.Abs(QuantityMultiplier - 1.0f) < 0.001f
                           && SuccessChanceBonus == 0
                           && TimeReductionRounds == 0;

    /// <summary>
    /// Aggregates a list of <see cref="CraftingModifier"/>s into a single set of net modifiers.
    /// <para>
    /// Processing order per step:
    /// <list type="number">
    ///   <item><description><see cref="EffectOperation.Additive"/> — summed into a running total</description></item>
    ///   <item><description><see cref="EffectOperation.PercentMult"/> — multiplied against the running total</description></item>
    ///   <item><description><see cref="EffectOperation.Subtractive"/> — subtracted from the running total</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public static AggregatedCraftingModifiers Aggregate(List<CraftingModifier> modifiers)
    {
        if (modifiers.Count == 0) return None;

        float qualityBonus = AggregateStep(modifiers, CraftingStep.Quality, 0f);
        float quantityMult = AggregateStep(modifiers, CraftingStep.Quantity, 1f);
        float successBonus = AggregateStep(modifiers, CraftingStep.SuccessChance, 0f);
        float timeReduction = AggregateStep(modifiers, CraftingStep.CraftingTime, 0f);

        return new AggregatedCraftingModifiers
        {
            QualityBonus = (int)Math.Round(qualityBonus),
            QuantityMultiplier = Math.Max(0f, quantityMult),
            SuccessChanceBonus = successBonus,
            TimeReductionRounds = (int)Math.Round(timeReduction)
        };
    }

    /// <summary>
    /// Processes all modifiers for a given step in order: additive → percent → subtractive.
    /// </summary>
    private static float AggregateStep(List<CraftingModifier> modifiers, CraftingStep step, float baseValue)
    {
        List<CraftingModifier> relevant = modifiers.Where(m => m.StepModified == step).ToList();
        if (relevant.Count == 0) return baseValue;

        float value = baseValue;

        // Phase 1: Additive
        foreach (CraftingModifier m in relevant.Where(m => m.Operation == EffectOperation.Additive))
        {
            value += m.Value;
        }

        // Phase 2: PercentMult
        foreach (CraftingModifier m in relevant.Where(m => m.Operation == EffectOperation.PercentMult))
        {
            value *= m.Value;
        }

        // Phase 3: Subtractive
        foreach (CraftingModifier m in relevant.Where(m => m.Operation == EffectOperation.Subtractive))
        {
            value -= m.Value;
        }

        return value;
    }
}
