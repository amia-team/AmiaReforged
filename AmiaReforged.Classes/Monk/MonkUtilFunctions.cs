// Utility/helper functions for monk stuff

using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class MonkUtilFunctions
{
    /// <summary>
    ///     Returns the monk's path type
    /// </summary>
    public static PathType? GetMonkPath(NwCreature monk)
    {
        NwFeat? pathFeat = monk.Feats.FirstOrDefault(feat => feat.Id is MonkFeat.CrashingMeteor
            or MonkFeat.SwingingCenser or MonkFeat.CrystalTides or MonkFeat.ChardalynSand
            or MonkFeat.IroncladBull or MonkFeat.CrackedVessel or MonkFeat.EchoingValley);

        return pathFeat?.Id switch
        {
            MonkFeat.CrashingMeteor => PathType.CrashingMeteor,
            MonkFeat.SwingingCenser => PathType.SwingingCenser,
            MonkFeat.CrystalTides => PathType.CrystalTides,
            MonkFeat.ChardalynSand => PathType.ChardalynSand,
            MonkFeat.IroncladBull => PathType.IroncladBull,
            MonkFeat.CrackedVessel => PathType.CrackedVessel,
            MonkFeat.EchoingValley => PathType.EchoingValley,
            _ => null
        };
    }

    /// <summary>
    ///     DC 10 + half the monk's character level + the monk's wisdom modifier
    /// </summary>
    /// <returns>The monk ability DC</returns>
    public static int CalculateMonkDc(NwCreature monk) => 10 + monk.GetClassInfo(ClassType.Monk)!.Level / 3 +
                                                          monk.GetAbilityModifier(Ability.Wisdom);

    /// <summary>
    ///     Returns a vfx effect resized to your desired size
    /// </summary>
    /// <param name="visualEffect">The visual effect you want to resize</param>
    /// <param name="desiredSize">
    ///     The size you desire in meters (small 1.67, medium 3.33, large 5, huge 6.67, gargantuan 8.33,
    ///     colossal 10)
    /// </param>
    /// <returns></returns>
    public static Effect ResizedVfx(VfxType visualEffect, float desiredSize)
    {
        float vfxDefaultSize = visualEffect switch
        {
            VfxType.ImpFrostL or VfxType.ImpAcidS => 1f,
            VfxType.FnfLosEvil10 or (VfxType)1046 => RadiusSize.Medium,
            VfxType.FnfFireball => RadiusSize.Huge,
            VfxType.FnfElectricExplosion => RadiusSize.Gargantuan,
            VfxType.FnfHowlOdd or VfxType.FnfHowlMind => RadiusSize.Colossal,
            _ => RadiusSize.Large
        };

        float vfxScale = desiredSize / vfxDefaultSize;
        return Effect.VisualEffect(visualEffect, false, vfxScale);
    }

    /// <summary>
    ///     A simpler version of NWN's spellsIsTarget() adjusted to Amia's difficulty setting. Don't use if it doesn't simplify
    ///     AoE spell targeting.
    /// </summary>
    /// <param name="creaturesOnly">true if you want to only affect creatures</param>
    /// <param name="affectsSelf">true if you want to affect yourself</param>
    /// <param name="alliesOnly">true if you to affect only allies</param>
    /// <returns>Valid target for spell effect</returns>
    public static bool IsValidTarget(NwObject targetObject, NwCreature caster, bool creaturesOnly, bool affectsSelf,
        bool alliesOnly)
    {
        if (targetObject == caster) return affectsSelf;
        if (creaturesOnly)
        {
            if (targetObject is not NwCreature targetCreature) return false;
            if (alliesOnly)
            {
                if (caster.IsReactionTypeFriendly(targetCreature))
                    return true;
            }
            else if (caster.IsReactionTypeHostile(targetCreature))
            {
                return true;
            }
        }
        else if (targetObject is NwCreature targetCreature && !caster.IsReactionTypeFriendly(targetCreature)
                 || targetObject is NwPlaceable || targetObject is NwDoor)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Sends debug message to player as "DEBUG: {debugString1} {debugString2}", debugString2 is colored
    /// </summary>
    public static void MonkDebug(NwPlayer player, string debugString1, string debugString2)
    {
        if (!player.IsValid) return;
        if (player.ControlledCreature.GetObjectVariable<LocalVariableInt>(name: "monk_debug").Value != 1) return;

        debugString2.ColorString(MonkColors.MonkColorScheme);
        player.SendServerMessage($"DEBUG: {debugString1} {debugString2}");
    }

    /// <summary>
    ///     A helper function for elements monk, gets the damage type based on the chosen element.
    /// </summary>
    public static DamageType GetElementalType(NwCreature monk)
    {
        DamageType elementalType = monk.GetObjectVariable<LocalVariableInt>(MonkElemental.VarName).Value switch
        {
            MonkElemental.Fire => DamageType.Fire,
            MonkElemental.Water => DamageType.Cold,
            MonkElemental.Air => DamageType.Electrical,
            MonkElemental.Earth => DamageType.Acid,
            _ => DamageType.Fire
        };
        return elementalType;
    }
}