using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class FiendPactAppearance
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
        GenderId: 0,
        WingsId: (int)CreatureWingType.Dragon,
        WingsScale: 1.3f,
        TailId: 12,
        TailScale: 1.2f,
        PhenotypeId: (int)Phenotype.Normal,
        SkinColorId: 64,
        HairColorId: 23,
        TattooColorOneId: 103
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Male,
        AppearanceId = (int)AppearanceType.HalfOrc,
        HeadId = 40,
        TattooColorTwoId = 0,
        Scale = 1.1f
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Female,
        HeadId = 169,
        AppearanceId = (int)AppearanceType.Human,
        RightHandId = 203,
        LeftHandId = 203,
        Scale = 1.3f,
        TattooColorTwoId = 91
    };
}
