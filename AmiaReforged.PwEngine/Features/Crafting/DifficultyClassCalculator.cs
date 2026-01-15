using System.Reflection;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Crafting;

/// <summary>
///     Calculate the skill check required for crafting an item.
/// </summary>
[ServiceBinding(typeof(DifficultyClassCalculator))]
public class DifficultyClassCalculator
{
    public int ComputeDifficulty(CraftingProperty property)
    {
        // We use a "Hill function" (smooth S-curve) to calculate difficulty based on the item's crafting tier.
        // This creates a natural progression where:
        // - Low tier items (0-1) are relatively easy (DC 10-20)
        // - Mid tier items (2-3) ramp up quickly (DC 30-50)
        // - High tier items (4+) approach maximum difficulty (DC 60-70)
        // The curve ensures difficulty scales smoothly rather than jumping abruptly between tiers.

        int powerLevel = Math.Max(0, (int)property.CraftingTier);

        LogManager.GetCurrentClassLogger().Info($"[DC CALC DEBUG] ComputeDifficulty called for tier {property.CraftingTier}, powerLevel: {powerLevel}");

        // Define the difficulty range: easiest and hardest possible DCs
        const double minimumDifficulty = 10.0;  // Tier 0 items
        const double maximumDifficulty = 70.0;  // High tier items

        // The "midpoint" tier where difficulty is halfway between min and max (around DC 40)
        const double midpointTier = 2.7;

        // How steeply difficulty increases - higher values make the transition sharper
        const double curveSteepness = 2.0;

        // Calculate how far along the difficulty curve this tier falls (0.0 to 1.0)
        double powerLevelExponent = Math.Pow(powerLevel, curveSteepness);
        double midpointExponent = Math.Pow(midpointTier, curveSteepness);
        double scalingFactor = powerLevelExponent <= 0 ? 0.0 : powerLevelExponent / (powerLevelExponent + midpointExponent);

        LogManager.GetCurrentClassLogger().Info($"[DC CALC DEBUG]   powerLevel^steepness: {powerLevelExponent}");
        LogManager.GetCurrentClassLogger().Info($"[DC CALC DEBUG]   midpoint^steepness: {midpointExponent}");
        LogManager.GetCurrentClassLogger().Info($"[DC CALC DEBUG]   scalingFactor: {scalingFactor}");

        // Apply the scaling factor to get the final DC between min and max
        int difficultyClass = (int)Math.Floor(minimumDifficulty + (maximumDifficulty - minimumDifficulty) * scalingFactor);

        LogManager.GetCurrentClassLogger().Info($"[DC CALC DEBUG]   Final DC: {difficultyClass}");

        return difficultyClass;
    }
}

