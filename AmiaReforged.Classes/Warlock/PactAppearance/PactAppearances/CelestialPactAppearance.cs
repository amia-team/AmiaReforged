using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class CelestialPactAppearance
{
    public static ChangeAppearanceData GetAppearance(Gender gender)
    {
        return gender switch
        {
            Gender.Female => FemaleAppearance,
            _ => MaleAppearance
        };
    }

    private static ChangeAppearanceData SharedAppearance => new(
        AppearanceId: (int)AppearanceType.HalfOrc,
        PhenotypeId: (int)Phenotype.Normal,
        SkinColorId: 10,
        HairColorId: 49,
        TattooColorOneId: 63,
        WingsId: (int)CreatureWingType.Bird,
        Scale: 1.2f,
        WingsScale: 1.7f
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Male,
        HeadId = 36
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Female,
        HeadId = 17
    };
}
