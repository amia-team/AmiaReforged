using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class SlaadPactAppearance
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
        PhenotypeId: (int)Phenotype.Big,
        SkinColorId: 54,
        HairColorId: 155,
        Scale: 1.3f
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Male,
        HeadId = 232,
        TattooColorOneId = 168,
        TattooColorTwoId = 96
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        GenderId = (int)Gender.Female,
        HeadId = 101,
        TattooColorOneId = 96,
        TattooColorTwoId = 168
    };
}
