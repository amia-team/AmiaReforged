// Applies the path effects to the techniques

using AmiaReforged.Classes.Monk.Types;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class AugmentationApplier
{
    /// <summary>
    ///     This routes the event data to the correct path effects
    /// </summary>
    /// <param name="path">Path from GetMonkPath()</param>
    /// <param name="technique">Technique from which this function was called</param>
    /// <param name="castData">Use for body and spirit techniques</param>
    /// <param name="wholenessData">Separate for Wholeness of Body, as an instant spell it uses other event data</param>
    /// <param name="attackData">Use for martial techniques</param>
    public static void ApplyAugmentations(PathType? path, TechniqueType technique, OnSpellCast? castData = null, 
        OnCreatureAttack? attackData = null)
    {
        switch (path)
        {
            case PathType.CrashingMeteor:
                CrashingMeteor.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.SwingingCenser:
                SwingingCenser.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.CrystalTides:
                CrystalTides.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.FickleStrand:
                FickleStrand.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.IroncladBull:
                IroncladBull.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.CrackedVessel:
                CrackedVessel.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.EchoingValley:
                EchoingValley.ApplyAugmentations(technique, castData, attackData);
                break;
            default:
                return;
        }
    }
}