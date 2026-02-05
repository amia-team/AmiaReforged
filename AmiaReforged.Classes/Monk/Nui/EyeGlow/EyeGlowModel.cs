using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.EyeGlow;

public class EyeGlowModel
{
    // Custom blue vfx constants
    private const VfxType EyesBlueHumanMale = (VfxType)324;
    private const VfxType EyesBlueHumanFemale = (VfxType)325;
    private const VfxType EyesBlueDwarfMale = (VfxType)326;
    private const VfxType EyesBlueDwarfFemale = (VfxType)327;
    private const VfxType EyesBlueElfMale = (VfxType)328;
    private const VfxType EyesBlueElfFemale = (VfxType)329;
    private const VfxType EyesBlueGnomeMale = (VfxType)330;
    private const VfxType EyesBlueGnomeFemale = (VfxType)331;
    private const VfxType EyesBlueHalflingMale = (VfxType)332;
    private const VfxType EyesBlueHalflingFemale = (VfxType)333;
    private const VfxType EyesBlueHalforcMale = (VfxType)334;
    private const VfxType EyesBlueHalforcFemale = (VfxType)335;

    public const string PermanentGlowTag = "monk_perm_eye_glow";
    public const string TemporaryGlowTag = "monk_temp_eye_glow";

    public VfxType? GetVfx(EyeGlowType type, NwCreature monk)
    {
        Gender gender = monk.Gender;
        AppearanceType appearance = (AppearanceType)monk.Appearance.RowIndex;

        return type switch
        {
            EyeGlowType.Cyan => Map(gender, appearance, VfxType.EyesCynHumanMale, VfxType.EyesCynHumanFemale, VfxType.EyesCynDwarfMale, VfxType.EyesCynDwarfFemale, VfxType.EyesCynElfMale, VfxType.EyesCynElfFemale, VfxType.EyesCynGnomeMale, VfxType.EyesCynGnomeFemale, VfxType.EyesCynHalflingMale, VfxType.EyesCynHalflingFemale, VfxType.EyesCynHalforcMale, VfxType.EyesCynHalforcFemale),
            EyeGlowType.Green => Map(gender, appearance, VfxType.EyesGreenHumanMale, VfxType.EyesGreenHumanFemale, VfxType.EyesGreenDwarfMale, VfxType.EyesGreenDwarfFemale, VfxType.EyesGreenElfMale, VfxType.EyesGreenElfFemale, VfxType.EyesGreenGnomeMale, VfxType.EyesGreenGnomeFemale, VfxType.EyesGreenHalflingMale, VfxType.EyesGreenHalflingFemale, VfxType.EyesGreenHalforcMale, VfxType.EyesGreenHalforcFemale),
            EyeGlowType.Yellow => Map(gender, appearance, VfxType.EyesYelHumanMale, VfxType.EyesYelHumanFemale, VfxType.EyesYelDwarfMale, VfxType.EyesYelDwarfFemale, VfxType.EyesYelElfMale, VfxType.EyesYelElfFemale, VfxType.EyesYelGnomeMale, VfxType.EyesYelGnomeFemale, VfxType.EyesYelHalflingMale, VfxType.EyesYelHalflingFemale, VfxType.EyesYelHalforcMale, VfxType.EyesYelHalforcFemale),
            EyeGlowType.White => Map(gender, appearance, VfxType.EyesWhtHumanMale, VfxType.EyesWhtHumanFemale, VfxType.EyesWhtDwarfMale, VfxType.EyesWhtDwarfFemale, VfxType.EyesWhtElfMale, VfxType.EyesWhtElfFemale, VfxType.EyesWhtGnomeMale, VfxType.EyesWhtGnomeFemale, VfxType.EyesWhtHalflingMale, VfxType.EyesWhtHalflingFemale, VfxType.EyesWhtHalforcMale, VfxType.EyesWhtHalforcFemale),
            EyeGlowType.Orange => Map(gender, appearance, VfxType.EyesOrgHumanMale, VfxType.EyesOrgHumanFemale, VfxType.EyesOrgDwarfMale, VfxType.EyesOrgDwarfFemale, VfxType.EyesOrgElfMale, VfxType.EyesOrgElfFemale, VfxType.EyesOrgGnomeMale, VfxType.EyesOrgGnomeFemale, VfxType.EyesOrgHalflingMale, VfxType.EyesOrgHalflingFemale, VfxType.EyesOrgHalforcMale, VfxType.EyesOrgHalforcFemale),
            EyeGlowType.Purple => Map(gender, appearance, VfxType.EyesPurHumanMale, VfxType.EyesPurHumanFemale, VfxType.EyesPurDwarfMale, VfxType.EyesPurDwarfFemale, VfxType.EyesPurElfMale, VfxType.EyesPurElfFemale, VfxType.EyesPurGnomeMale, VfxType.EyesPurGnomeFemale, VfxType.EyesPurHalflingMale, VfxType.EyesPurHalflingFemale, VfxType.EyesPurHalforcMale, VfxType.EyesPurHalforcFemale),
            EyeGlowType.Flame => Map(gender, appearance, VfxType.EyesRedFlameHumanMale, VfxType.EyesRedFlameHumanFemale, VfxType.EyesRedFlameDwarfMale, VfxType.EyesRedFlameDwarfFemale, VfxType.EyesRedFlameElfMale, VfxType.EyesRedFlameElfFemale, VfxType.EyesRedFlameGnomeMale, VfxType.EyesRedFlameGnomeFemale, VfxType.EyesRedFlameHalflingMale, VfxType.EyesRedFlameHalflingFemale, VfxType.EyesRedFlameHalforcMale, VfxType.EyesRedFlameHalforcFemale),
            EyeGlowType.Blue => Map(gender, appearance, EyesBlueHumanMale, EyesBlueHumanFemale, EyesBlueDwarfMale, EyesBlueDwarfFemale, EyesBlueElfMale, EyesBlueElfFemale, EyesBlueGnomeMale, EyesBlueGnomeFemale, EyesBlueHalflingMale, EyesBlueHalflingFemale, EyesBlueHalforcMale, EyesBlueHalforcFemale),
            _ => null
        };
    }

    private static VfxType? Map(Gender gender, AppearanceType appearance, VfxType humanMale, VfxType humanFemale,
        VfxType dwarfMale, VfxType dwarfFemale, VfxType elfMale, VfxType elfFemale, VfxType gnomeMale,
        VfxType gnomeFemale, VfxType halflingMale, VfxType halflingFemale, VfxType halfOrcMale, VfxType halfOrcFemale)
    {
        return (gender, appearance) switch
        {
            (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => humanMale,
            (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => humanFemale,
            (Gender.Male, AppearanceType.Dwarf) => dwarfMale,
            (Gender.Female, AppearanceType.Dwarf) => dwarfFemale,
            (Gender.Male, AppearanceType.Elf) => elfMale,
            (Gender.Female, AppearanceType.Elf) => elfFemale,
            (Gender.Male, AppearanceType.Gnome) => gnomeMale,
            (Gender.Female, AppearanceType.Gnome) => gnomeFemale,
            (Gender.Male, AppearanceType.Halfling) => halflingMale,
            (Gender.Female, AppearanceType.Halfling) => halflingFemale,
            (Gender.Male, AppearanceType.HalfOrc) => halfOrcMale,
            (Gender.Female, AppearanceType.HalfOrc) => halfOrcFemale,
            _ => null
        };
    }
}
