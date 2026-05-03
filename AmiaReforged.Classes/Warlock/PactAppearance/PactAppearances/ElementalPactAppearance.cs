using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class ElementalPactAppearance
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
        SkinColorId: 57,
        TattooColorOneId: 153,
        TattooColorTwoId: 153,
        LeftForearmId: 1,
        RightForearmId: 1,
        LeftBicepId: 1,
        RightBicepId: 1,
        TorsoId: 1,
        LeftThighId: 1,
        RightThighId: 1,
        LeftShinId: 1,
        RightShinId: 1
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Male,
        HeadId = 101,
        HairColorId = 153,
        Scale = 0.9f
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Female,
        HeadId = 163,
        HairColorId = 57,
        Scale = 1.0f
    };
}
