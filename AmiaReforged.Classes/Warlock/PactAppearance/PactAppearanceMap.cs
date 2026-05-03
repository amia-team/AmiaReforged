using AmiaReforged.Classes.EffectUtils.ChangeAppearance;
using AmiaReforged.Classes.Warlock.PactAppearance.PactAppearances;
using AmiaReforged.Classes.Warlock.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.PactAppearance;

public static class PactAppearanceMap
{
    public static ChangeAppearanceData? GetAppearance(PactType pactType, Gender gender) => pactType switch
    {
        PactType.Aberrant => AberrantPactAppearance.GetAppearance(gender),
        PactType.Celestial => CelestialPactAppearance.GetAppearance(gender),
        PactType.Fey => FeyPactAppearance.GetAppearance(gender),
        PactType.Fiend => FiendPactAppearance.GetAppearance(gender),
        PactType.Elemental => ElementalPactAppearance.GetAppearance(gender),
        PactType.Slaad => SlaadPactAppearance.GetAppearance(gender),
        _ => null
    };
}
