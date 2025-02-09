// Applies the path effects to the techniques
using AmiaReforged.Classes.Monk.Effects;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Types;

public static class PathEffectApplier
{
    /// <summary>
    /// This routes the event data to the correct path effects
    /// </summary>
    /// <param name="path"></param> Path from GetMonkPath()
    /// <param name="technique"></param> Technique from which this function was called
    /// <param name="castData"></param> Use for body and spirit techniques
    /// <param name="attackData"></param> Use for martial techniques

    public static void ApplyPathEffects(PathType? path, TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (path)
        {
            case PathType.Elements : ElementsPathEffects.ApplyElementsPathEffects(technique, castData, attackData);
                break;
            case PathType.Hymn : 
                break;
            case PathType.Clarity : 
                break;
            case PathType.Mantle : 
                break;
            case PathType.Golem : 
                break;
            case PathType.Torment : 
                break;
            case PathType.Mists :
                break;
            default: return;
        }
    }
}