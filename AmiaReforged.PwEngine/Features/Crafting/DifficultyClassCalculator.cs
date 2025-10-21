using System.Reflection;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Crafting;

/// <summary>
///     Calculate the skill check required for crafting an item.
/// </summary>
[ServiceBinding(typeof(DifficultyClassCalculator))]
public class DifficultyClassCalculator
{
    public int ComputeDifficulty(CraftingProperty property)
    {
        // Hill function (a.k.a. Naka–Rushton): saturating curve with half-saturation K and steepness n
        // DC = floor(Lmin + (Lmax − Lmin) * P^n / (P^n + K^n))
        int p = Math.Max(0, (int)property.CraftingTier);
        const double lmin = 10.0;
        const double lmax = 70.0;
        const double k = 2.7;
        const double n = 2.0;

        double pn = Math.Pow(p, n);
        double kn = Math.Pow(k, n);
        double frac = pn <= 0 ? 0.0 : pn / (pn + kn);
        int dc = (int)Math.Floor(lmin + (lmax - lmin) * frac);
        return dc;
    }
}

