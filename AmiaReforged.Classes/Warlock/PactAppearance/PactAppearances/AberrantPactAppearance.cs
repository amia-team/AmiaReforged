using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;

public static class AberrantPactAppearance
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
        GenderId: (int)Gender.Male,
        AppearanceId: (int)AppearanceType.Elf,
        PhenotypeId: (int)Phenotype.Normal,
        HeadId: 30,
        SkinColorId: 71,
        TattooColorOneId: 72,
        RightHandId: 202,
        LeftHandId: 202
    );

    private static ChangeAppearanceData MaleAppearance => SharedAppearance with
    {
        // male overrides
    };

    private static ChangeAppearanceData FemaleAppearance => SharedAppearance with
    {
        // female overrides
    };
}
