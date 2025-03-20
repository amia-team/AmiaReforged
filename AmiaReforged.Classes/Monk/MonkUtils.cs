// Utility/helper functions for monk stuff

using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk;

public static class MonkUtils
{
    /// <summary>
    ///     Returns the monk's path type
    /// </summary>
    public static PathType? GetMonkPath(NwCreature monk)
    {
        // Check for Path of Enlightenment PrC as a failsafe
        if (monk.GetClassInfo(NwClass.FromClassId(50)) is not null) return null;
            
        NwFeat? pathFeat = monk.Feats.FirstOrDefault(feat => feat.Id is MonkFeat.CrashingMeteor
            or MonkFeat.SwingingCenser or MonkFeat.HiddenSpring or MonkFeat.FickleStrand
            or MonkFeat.IroncladBull or MonkFeat.CrackedVessel or MonkFeat.EchoingValley);

        return pathFeat?.Id switch
        {
            MonkFeat.CrashingMeteor => PathType.CrashingMeteor,
            MonkFeat.SwingingCenser => PathType.SwingingCenser,
            MonkFeat.HiddenSpring => PathType.HiddenSpring,
            MonkFeat.FickleStrand => PathType.FickleStrand,
            MonkFeat.IroncladBull => PathType.IroncladBull,
            MonkFeat.CrackedVessel => PathType.CrackedVessel,
            MonkFeat.EchoingValley => PathType.EchoingValley,
            _ => null
        };
    }
    
    /// <summary>
    /// Use in tandem with GetMonkPath, ie if GetMonkPath is not null, you can get the KiFocus. UPDATE TO USE MONK FEATS WHEN IMPLEMENTED!!!
    /// </summary>
    /// <returns>Ki Focus tier for scaling monk powers</returns>
    public static KiFocus? GetKiFocus(NwCreature monk)
    {
        return monk.GetClassInfo(ClassType.Monk)!.Level switch
        {
            >= 18 and < 24 => KiFocus.KiFocus1,
            >= 24 and < 30 => KiFocus.KiFocus2,
            30 => KiFocus.KiFocus3,
            _ => null
        };
    }

    /// <summary>
    ///     DC 10 + monk level / 3 + wisdom modifier
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
            VfxType.ImpFrostL or VfxType.ImpAcidS or VfxType.ImpBlindDeafM => 1f,
            VfxType.FnfLosEvil10 or (VfxType)1046 => RadiusSize.Medium,
            VfxType.FnfFireball => RadiusSize.Huge,
            VfxType.FnfElectricExplosion => RadiusSize.Gargantuan,
            VfxType.FnfHowlOdd or VfxType.FnfHowlMind or VfxType.FnfLosEvil30 => RadiusSize.Colossal,
            _ => RadiusSize.Large
        };

        float vfxScale = desiredSize / vfxDefaultSize;
        return Effect.VisualEffect(visualEffect, false, vfxScale);
    }

    /// <summary>
    ///     Sends debug message to player as "DEBUG: {debugString1} {debugString2}", debugString2 is colored
    /// </summary>
    public static void MonkDebug(NwPlayer player, string debugString1, string debugString2)
    {
        if (!player.IsValid) return;
        if (player.ControlledCreature.GetObjectVariable<LocalVariableInt>(name: "monk_debug").Value != 1) return;
        
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