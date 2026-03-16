namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Defines the cost curve for earning economy knowledge points through progression.
/// The curve determines how many progression points are needed for each successive
/// economy-earned knowledge point.
/// 
/// Three curve types are supported:
/// <list type="bullet">
///   <item><b>Linear</b>: Cost = BaseCost + (n * ScalingFactor). Steady, predictable growth.</item>
///   <item><b>Polynomial</b>: Cost = BaseCost + (n^2 * ScalingFactor). Moderate acceleration.</item>
///   <item><b>Exponential</b>: Cost = BaseCost * (ScalingFactor ^ n). Aggressive growth.</item>
/// </list>
/// 
/// Beyond the soft cap, an additional penalty multiplier is applied to make earning very tedious.
/// At or beyond the hard cap, earning is blocked entirely.
/// </summary>
public class ProgressionCurveConfig
{
    /// <summary>
    /// Base progression points needed for the first economy KP.
    /// </summary>
    public int BaseCost { get; set; } = 100;

    /// <summary>
    /// Scaling factor applied to the curve formula.
    /// Meaning varies by curve type (see class docs).
    /// </summary>
    public float ScalingFactor { get; set; } = 1.15f;

    /// <summary>
    /// The type of escalation curve.
    /// </summary>
    public ProgressionCurveType CurveType { get; set; } = ProgressionCurveType.Exponential;

    /// <summary>
    /// Economy-earned KP soft cap. Beyond this, costs are multiplied by <see cref="SoftCapPenaltyMultiplier"/>.
    /// </summary>
    public int SoftCap { get; set; } = 100;

    /// <summary>
    /// Economy-earned KP hard cap. At or beyond this, no more economy KP can be earned.
    /// </summary>
    public int HardCap { get; set; } = 150;

    /// <summary>
    /// Multiplier applied to progression costs when economy-earned KP exceeds the soft cap.
    /// </summary>
    public float SoftCapPenaltyMultiplier { get; set; } = 3.0f;

    /// <summary>
    /// Computes the progression point cost for the Nth economy-earned knowledge point (1-based).
    /// Returns <see cref="int.MaxValue"/> if the hard cap is reached or exceeded.
    /// </summary>
    /// <param name="n">The Nth economy KP being earned (1-based, e.g., 1 = first economy KP).</param>
    /// <param name="effectiveSoftCap">Soft cap to use (may be overridden by a cap profile).</param>
    /// <param name="effectiveHardCap">Hard cap to use (may be overridden by a cap profile).</param>
    /// <returns>Progression points required, or int.MaxValue if blocked by hard cap.</returns>
    public int CostForNthPoint(int n, int effectiveSoftCap, int effectiveHardCap)
    {
        if (n < 1) return 0;
        if (n > effectiveHardCap) return int.MaxValue;

        int baseCostForPoint = CurveType switch
        {
            ProgressionCurveType.Linear =>
                (int)Math.Ceiling(BaseCost + (n - 1) * ScalingFactor),

            ProgressionCurveType.Polynomial =>
                (int)Math.Ceiling(BaseCost + Math.Pow(n - 1, 2) * ScalingFactor),

            ProgressionCurveType.Exponential =>
                (int)Math.Ceiling(BaseCost * Math.Pow(ScalingFactor, n - 1)),

            _ => BaseCost
        };

        // Apply soft cap penalty if beyond the soft cap threshold
        if (n > effectiveSoftCap)
        {
            baseCostForPoint = (int)Math.Ceiling(baseCostForPoint * SoftCapPenaltyMultiplier);
        }

        return Math.Max(1, baseCostForPoint);
    }
}

/// <summary>
/// Curve type for knowledge point progression cost escalation.
/// </summary>
public enum ProgressionCurveType
{
    /// <summary>
    /// Cost = BaseCost + ((n-1) * ScalingFactor). Steady, predictable growth.
    /// </summary>
    Linear,

    /// <summary>
    /// Cost = BaseCost + ((n-1)^2 * ScalingFactor). Moderate acceleration.
    /// </summary>
    Polynomial,

    /// <summary>
    /// Cost = BaseCost * (ScalingFactor ^ (n-1)). Aggressive exponential growth.
    /// </summary>
    Exponential
}
