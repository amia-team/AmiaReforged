using AmiaReforged.Classes.Warlock.EldritchBlast.Essence;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.EldritchBlast;

public static class EldritchEyeGlow
{
    private const VfxType EyesRedHumanMale = (VfxType)710;
    private const VfxType EyesRedHumanFemale = (VfxType)738;
    private const VfxType EyesRedDwarfMale = (VfxType)823;
    private const VfxType EyesRedDwarfFemale = (VfxType)851;
    private const VfxType EyesRedElfMale = (VfxType)766;
    private const VfxType EyesRedElfFemale = (VfxType)795;
    private const VfxType EyesRedHalflingMale = (VfxType)879;
    private const VfxType EyesRedHalflingFemale = (VfxType)907;
    private const VfxType EyesRedHalfOrcMale = (VfxType)991;
    private const VfxType EyesRedHalfOrcFemale = (VfxType)1019;

    private static readonly Dictionary<(EssenceType, AppearanceType, Gender), VfxType> EyeGlowLookup = new();

    static EldritchEyeGlow()
    {
        EyeInfo(EssenceType.Brimstone, VfxType.EyesRedFlameHumanMale, VfxType.EyesRedFlameHumanFemale, VfxType.EyesRedFlameHalfelfMale, VfxType.EyesRedFlameHalfelfFemale, VfxType.EyesRedFlameDwarfMale, VfxType.EyesRedFlameDwarfFemale, VfxType.EyesRedFlameElfMale, VfxType.EyesRedFlameElfFemale, VfxType.EyesRedFlameHalflingMale, VfxType.EyesRedFlameHalflingFemale, VfxType.EyesRedFlameHalforcMale, VfxType.EyesRedFlameHalforcFemale);
        EyeInfo(EssenceType.Utterdark, EyesRedHumanMale, EyesRedHumanFemale, EyesRedHumanMale, EyesRedHumanFemale, EyesRedDwarfMale, EyesRedDwarfFemale, EyesRedElfMale, EyesRedElfFemale, EyesRedHalflingMale, EyesRedHalflingFemale, EyesRedHalfOrcMale, EyesRedHalfOrcFemale);
        EyeInfo(EssenceType.Vitriolic, VfxType.EyesGreenHumanMale, VfxType.EyesGreenHumanFemale, VfxType.EyesGreenHalfelfMale, VfxType.EyesGreenHalfelfFemale, VfxType.EyesGreenDwarfMale, VfxType.EyesGreenDwarfFemale, VfxType.EyesGreenElfMale, VfxType.EyesGreenElfFemale, VfxType.EyesGreenHalflingMale, VfxType.EyesGreenHalflingFemale, VfxType.EyesGreenHalforcMale, VfxType.EyesGreenHalforcFemale);
        EyeInfo(EssenceType.Hellrime, VfxType.EyesCynHumanMale, VfxType.EyesCynHumanFemale, VfxType.EyesCynHumanMale, VfxType.EyesCynHumanFemale, VfxType.EyesCynDwarfMale, VfxType.EyesCynDwarfFemale, VfxType.EyesCynElfMale, VfxType.EyesCynElfFemale, VfxType.EyesCynHalflingMale, VfxType.EyesCynHalflingFemale, VfxType.EyesCynHalforcMale, VfxType.EyesCynHalforcFemale);
        EyeInfo(EssenceType.Screaming, VfxType.EyesWhtHumanMale, VfxType.EyesWhtHumanFemale, VfxType.EyesWhtHumanMale, VfxType.EyesWhtHumanFemale, VfxType.EyesWhtDwarfMale, VfxType.EyesWhtDwarfFemale, VfxType.EyesWhtElfMale, VfxType.EyesWhtElfFemale, VfxType.EyesWhtHalflingMale, VfxType.EyesWhtHalflingFemale, VfxType.EyesWhtHalforcMale, VfxType.EyesWhtHalforcFemale);
        EyeInfo(default, VfxType.EyesPurHumanMale, VfxType.EyesPurHumanFemale, VfxType.EyesPurHumanMale, VfxType.EyesPurHumanFemale, VfxType.EyesPurDwarfMale, VfxType.EyesPurDwarfFemale, VfxType.EyesPurElfMale, VfxType.EyesPurElfFemale, VfxType.EyesPurHalflingMale, VfxType.EyesPurHalflingFemale, VfxType.EyesPurHalforcMale, VfxType.EyesPurHalforcFemale);
    }

    public static void ApplyEyeGlow(NwCreature creature, EssenceType essence, AppearanceType appearance, Gender gender)
    {
        VfxType eyeGlow = GetEssenceEyeGlow(essence, appearance, gender);
        if (eyeGlow == VfxType.None) return;

        Effect vfx = Effect.VisualEffect(eyeGlow, fScale: creature.VisualTransform.Scale);
        creature.ApplyEffect(EffectDuration.Temporary, vfx, NwTimeSpan.FromRounds(1));
    }

    private static VfxType GetEssenceEyeGlow(EssenceType essence, AppearanceType appearance, Gender gender)
    {
        // Ew Amia gnomes are halflings ew
        AppearanceType normalizedAppearance = appearance switch
        {
            AppearanceType.Gnome => AppearanceType.Halfling,
            _ => appearance
        };

        return EyeGlowLookup.TryGetValue((essence, normalizedAppearance, gender), out VfxType result) ? result
            : EyeGlowLookup.GetValueOrDefault((default, normalizedAppearance, gender), VfxType.None);
    }

    private static void EyeInfo(EssenceType essence, VfxType hM, VfxType hF, VfxType heM, VfxType heF, VfxType dM, VfxType dF, VfxType eM, VfxType eF, VfxType haM, VfxType haF, VfxType hoM, VfxType hoF)
    {
        EyeGlowLookup[(essence, AppearanceType.Human, Gender.Male)] = hM;
        EyeGlowLookup[(essence, AppearanceType.Human, Gender.Female)] = hF;
        EyeGlowLookup[(essence, AppearanceType.HalfElf, Gender.Male)] = heM;
        EyeGlowLookup[(essence, AppearanceType.HalfElf, Gender.Female)] = heF;
        EyeGlowLookup[(essence, AppearanceType.Dwarf, Gender.Male)] = dM;
        EyeGlowLookup[(essence, AppearanceType.Dwarf, Gender.Female)] = dF;
        EyeGlowLookup[(essence, AppearanceType.Elf, Gender.Male)] = eM;
        EyeGlowLookup[(essence, AppearanceType.Elf, Gender.Female)] = eF;
        EyeGlowLookup[(essence, AppearanceType.Halfling, Gender.Male)] = haM;
        EyeGlowLookup[(essence, AppearanceType.Halfling, Gender.Female)] = haF;
        EyeGlowLookup[(essence, AppearanceType.HalfOrc, Gender.Male)] = hoM;
        EyeGlowLookup[(essence, AppearanceType.HalfOrc, Gender.Female)] = hoF;
    }
}
