using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

public struct CrashingMeteorData
{
    public int Dc;
    public int DiceAmount;
    public short BonusDamage;
    public Effect AoeVfx;
    public Effect PulseVfx;
    public VfxType DamageVfx;
    public DamageType DamageType;
    public SavingThrowType SaveType;
    public int DamageVulnerability;

    public static CrashingMeteorData GetCrashingMeteorData(NwCreature monk)
    {
        ElementalType elementalType = MonkUtils.GetElementalTypeVar(monk).Value;

        return new CrashingMeteorData
        {
            Dc = MonkUtils.CalculateMonkDc(monk),
            DiceAmount = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 4,
                KiFocus.KiFocus2 => 6,
                KiFocus.KiFocus3 => 8,
                _ => 2
            },
            BonusDamage = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 2,
                KiFocus.KiFocus2 => 3,
                KiFocus.KiFocus3 => 4,
                _ => 1
            },
            AoeVfx = MonkUtils.ResizedVfx(elementalType switch
            {
                ElementalType.Fire => VfxType.FnfFireball,
                ElementalType.Water => MonkVfx.FnfFreezingSphere,
                ElementalType.Air => VfxType.FnfElectricExplosion,
                ElementalType.Earth => MonkVfx.FnfVitriolicSphere,
                _ => VfxType.FnfFireball
            }, RadiusSize.Large),
            PulseVfx = MonkUtils.ResizedVfx(elementalType switch
            {
                ElementalType.Fire => MonkVfx.ImpPulseFireChest,
                ElementalType.Water => MonkVfx.ImpPulseColdChest,
                ElementalType.Air => MonkVfx.ImpPulseAirChest,
                ElementalType.Earth => MonkVfx.ImpPulseEarthChest,
                _ => MonkVfx.ImpPulseFireChest
            }, RadiusSize.Medium),
            DamageVfx = elementalType switch
            {
                ElementalType.Fire => VfxType.ImpFlameS,
                ElementalType.Water => VfxType.ImpFrostS,
                ElementalType.Air => VfxType.ComHitElectrical,
                ElementalType.Earth => VfxType.ImpAcidS,
                _ => VfxType.ImpFlameS
            },
            DamageType = elementalType switch
            {
                ElementalType.Fire => DamageType.Fire,
                ElementalType.Water => DamageType.Cold,
                ElementalType.Air => DamageType.Electrical,
                ElementalType.Earth => DamageType.Acid,
                _ => DamageType.Fire
            },
            SaveType = elementalType switch
            {
                ElementalType.Fire => SavingThrowType.Fire,
                ElementalType.Water => SavingThrowType.Cold,
                ElementalType.Air => SavingThrowType.Electricity,
                ElementalType.Earth => SavingThrowType.Acid,
                _ => SavingThrowType.Fire
            },
            DamageVulnerability = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 10,
                KiFocus.KiFocus2 => 15,
                KiFocus.KiFocus3 => 20,
                _ => 5
            }
        };
    }
}
