using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;
using AmiaReforged.Classes.Monk.Constants;

namespace AmiaReforged.Classes.Monk;

public static class MonkUtils
{
    private const string ElementalVarName = "monk_elemental_type";
    private static readonly NwClass? ObsoletePoeClass = NwClass.FromClassId(50);

    /// <summary>
    ///     Returns the monk's path type
    /// </summary>
    public static PathType? GetMonkPath(NwCreature monk)
    {
        // Check for Path of Enlightenment PrC as a failsafe
        if (monk.GetClassInfo(ObsoletePoeClass) is not null) return null;

        foreach (PathType path in Enum.GetValues<PathType>())
        {
            if (monk.KnowsFeat(NwFeat.FromFeatId((int)path)!))
                return path;
        }

        return null;
    }

    /// <summary>
    /// Use in tandem with GetMonkPath, ie if GetMonkPath is not null, you can get the KiFocus.
    /// </summary>
    /// <returns>Ki Focus tier for scaling monk powers</returns>
    public static KiFocus? GetKiFocus(NwCreature monk)
    {
        if (monk.KnowsFeat(((Feat)KiFocus.KiFocus3)!))
            return KiFocus.KiFocus3;
        if (monk.KnowsFeat(((Feat)KiFocus.KiFocus2)!))
            return KiFocus.KiFocus2;
        if (monk.KnowsFeat(((Feat)KiFocus.KiFocus1)!))
            return KiFocus.KiFocus1;

        return null;
    }

    /// <summary>
    ///     DC 10 + monk level / 3 + wisdom modifier
    /// </summary>
    /// <returns>The monk ability DC</returns>
    public static int CalculateMonkDc(NwCreature monk)
        => 10 + NWScript.GetLevelByClass((int)ClassType.Monk, monk) / 3 + monk.GetAbilityModifier(Ability.Wisdom);

    /// <summary>
    ///     Resizes a vfx to your desired size
    /// </summary>
    /// <param name="vfxType">The visual effect you want to resize</param>
    /// <param name="desiredSize">
    ///     The size you desire in meters (small 1.67, medium 3.33, large 5, huge 6.67, gargantuan 8.33,
    ///     colossal 10)
    /// </param>
    /// <returns>The resized Effect.VisualEffect</returns>
    public static Effect ResizedVfx(VfxType vfxType, float desiredSize)
    {
        float vfxDefaultSize = vfxType switch
        {
            VfxType.ImpFrostL => 0.7f,
            VfxType.ImpAcidS or VfxType.ImpBlindDeafM => 1f,
            VfxType.FnfLosEvil10 => RadiusSize.Medium,
            VfxType.FnfElectricExplosion or MonkVfx.FnfFreezingSphere or MonkVfx.FnfVitriolicSphere
                or VfxType.FnfMysticalExplosion => RadiusSize.Gargantuan,
            VfxType.FnfHowlOdd or VfxType.FnfHowlMind or VfxType.FnfLosEvil30 or VfxType.FnfLosHoly30 => RadiusSize.Colossal,
            _ => RadiusSize.Large
        };

        float vfxScale = desiredSize / vfxDefaultSize;
        return Effect.VisualEffect(vfxType, false, vfxScale);
    }

    /// <summary>
    /// A helper function for elements monk, gets the elemental type local variable whose value is used to switch the type.
    /// </summary>
    public static LocalVariableEnum<ElementalType> GetElementalTypeVar(NwCreature monk)
    {
        return monk.GetObjectVariable<LocalVariableEnum<ElementalType>>(ElementalVarName);
    }

    public static int GetCritMultiplier(OnCreatureAttack attackData, NwCreature monk)
    {
        byte baseMultiplier = attackData.WeaponAttackType switch
        {
            WeaponAttackType.Unarmed or WeaponAttackType.UnarmedExtra or WeaponAttackType.CreatureBite
                or WeaponAttackType.CreatureLeft or WeaponAttackType.CreatureRight
                => 2,
            WeaponAttackType.MainHand or WeaponAttackType.HastedAttack
                => monk.GetItemInSlot(InventorySlot.RightHand)?.BaseItem.CritMultiplier ?? 2,
            WeaponAttackType.Offhand
                => monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.CritMultiplier ?? 2,
            _ => 2
        };

        if (attackData.IsRangedAttack) return baseMultiplier;

        if (monk.KnowsFeat(Feat.IncreaseMultiplier!)
            && attackData.WeaponAttackType is WeaponAttackType.MainHand or WeaponAttackType.Offhand)
            return baseMultiplier + 1;

        return baseMultiplier;
    }

    public static bool AbilityRestricted(NwCreature monk, string disabledWhat)
    {
        bool hasArmor = monk.HasArmor();
        bool hasShield = monk.HasShield();

        if (!monk.IsPlayerControlled(out NwPlayer? player)) return hasArmor || hasShield;

        if (hasArmor)
        {
            player.SendServerMessage($"Equipping this armor has disabled your {disabledWhat}.");
            return hasArmor;
        }

        if (hasShield)
        {
            player.SendServerMessage($"Equipping this shield has disabled your {disabledWhat}.");
            return hasShield;
        }

        return false;
    }

    private static bool HasArmor(this NwCreature monk) =>
        monk.GetItemInSlot(InventorySlot.Chest)?.BaseACValue > 0;

    private static bool HasShield(this NwCreature monk) =>
        monk.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.Category == BaseItemCategory.Shield;

    public static async Task GetObjectContext(NwCreature monk, Effect effect)
    {
        await monk.WaitForObjectContext();
        Effect awaitedEffect = effect;
    }
}
