// Applies the path effects to the techniques

using AmiaReforged.Classes.Monk.Types;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class AugmentationApplier
{
    /// <summary>
    ///     This routes the event data to the correct path effects
    /// </summary>
    /// <param name="path"></param>
    /// Path from GetMonkPath()
    /// <param name="technique"></param>
    /// Technique from which this function was called
    /// <param name="castData"></param>
    /// Use for body and spirit techniques
    /// <param name="attackData"></param>
    /// Use for martial techniques
    public static void ApplyAugmentations(PathType? path, TechniqueType technique, OnSpellCast? castData = null,
        OnCreatureAttack? attackData = null)
    {
        switch (path)
        {
            case PathType.Elements:
                CrashingMeteor.ApplyAugmentations(technique, castData, attackData);
                break;
            case PathType.Hymn:
                break;
            case PathType.Clarity:
                break;
            case PathType.Mantle:
                break;
            case PathType.Golem:
                break;
            case PathType.Torment:
                break;
            case PathType.Mists:
                break;
            default: return;
        }
    }
}