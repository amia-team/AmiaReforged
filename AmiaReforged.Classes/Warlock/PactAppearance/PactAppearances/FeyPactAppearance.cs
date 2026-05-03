using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class FeyPactAppearance
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
        AppearanceId: (int)AppearanceType.Human,
        PhenotypeId: (int)Phenotype.Normal,
        SkinColorId: 111,
        HairColorId: 155,
        TattooColorOneId: 37,
        Scale: 0.9f,
        RightHandId: 203,
        LeftHandId: 203,
        RightFootId: 196,
        LeftFootId: 196
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Male,
        HeadId = 49
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Female,
        HeadId = 44
    };
}
