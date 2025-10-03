using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(EyeGlowSelector))]
public class EyeGlowSelector
{
    private readonly Dictionary<int, string> _eyeGlowNames = new()
    {
        { EyeGlowCyan, "Cyan Eye Glow" },
        { EyeGlowGreen, "Green Eye Glow" },
        { EyeGlowYellow, "Yellow Eye Glow" },
        { EyeGlowWhite, "White Eye Glow" },
        { EyeGlowOrange, "Orange Eye Glow" },
        { EyeGlowPurple, "Purple Eye Glow" },
        { EyeGlowRedFlame, "Red Flame Eye Glow" },
        { EyeGlowBlue, "Blue Eye Glow" }
    };

    private readonly Logger _log = LogManager.GetCurrentClassLogger();

    private const string MonkEyeGlowTag = "monk_eye_glow";

    private const int EyeGlowCyan = 1;
    private const int EyeGlowGreen = 2;
    private const int EyeGlowYellow = 3;
    private const int EyeGlowWhite = 4;
    private const int EyeGlowOrange = 5;
    private const int EyeGlowPurple = 6;
    private const int EyeGlowRedFlame = 7;
    private const int EyeGlowBlue = 8;

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

    public EyeGlowSelector()
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += SelectEyeGlow;

        _log.Info("Monk Eye Glow Selector initialized.");
    }

    private void SelectEyeGlow(OnUseFeat eventData)
    {
        if (eventData.Feat.FeatType is not Feat.PerfectSelf) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature monk = eventData.Creature;

        ToggleEyeGlow(monk, player);
    }

    private void ToggleEyeGlow(NwCreature monk, NwPlayer player)
    {
        LocalVariableInt eyeGlowKey = monk.GetObjectVariable<LocalVariableInt>(MonkEyeGlowTag);
        Effect? eyeGlowEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == MonkEyeGlowTag);

        if (eyeGlowEffect != null)
        {
            // If the effect has already been added as permanent, the monk has already selected their eye glow
            if (eyeGlowEffect.DurationType == EffectDuration.Permanent)
            {
                player.SendServerMessage("You have already selected your monk eye glow.");
                return;
            }
            // Otherwise remove the old eye glow and cycle on to the next
            monk.RemoveEffect(eyeGlowEffect);
        }

        // Reset key to first choice if it goes past the last color
        eyeGlowKey.Value = eyeGlowKey.Value % 8 + 1;

        VfxType? eyeGlowVfx = GetEyeGlowVfx(eyeGlowKey.Value, monk);

        string eyeGlowName = _eyeGlowNames[eyeGlowKey.Value];

        ApplyEyeGlow(monk, eyeGlowVfx, eyeGlowName, player);
    }
    private void ApplyEyeGlow(NwCreature monk, VfxType? eyeGlowVfx, string eyeGlowName, NwPlayer player)
    {
        if (eyeGlowVfx == null)
        {
            player.FloatingTextString("No suitable eye glow found for this appearance.", false);
            return;
        }

        float scale = monk.VisualTransform.Scale;

        Effect eyeGlowEffect = Effect.VisualEffect(eyeGlowVfx!, fScale: scale);
        eyeGlowEffect.Tag = MonkEyeGlowTag;

        monk.ApplyEffect(EffectDuration.Temporary, eyeGlowEffect, NwTimeSpan.FromRounds(5));

        string eyeGlowMessage =
            $"To confirm {eyeGlowName} as your permanent option, enter this in the chat: ./confirmeyeglow";

        player.FloatingTextString(eyeGlowMessage, false);
    }

    private VfxType? GetEyeGlowVfx(int eyeGlowKey, NwCreature monk)
    {
        AppearanceType appearanceType = (AppearanceType)monk.Appearance.RowIndex;

        return eyeGlowKey switch
        {
            EyeGlowCyan =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesCynHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesCynHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesCynHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesCynHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesCynHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesCynHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesCynGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesCynGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesCynDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesCynDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesCynElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesCynElfFemale,
                    _ => null
                },
            EyeGlowGreen =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesGreenHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesGreenHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesGreenHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesGreenHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesGreenHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesGreenHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesGreenGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesGreenGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesGreenDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesGreenDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesGreenElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesGreenElfFemale,
                    _ => null
                },
            EyeGlowYellow =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesYelHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesYelHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesYelHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesYelHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesYelHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesYelHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesYelGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesYelGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesYelDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesYelDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesYelElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesYelElfFemale,
                    _ => null
                },
            EyeGlowWhite =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesWhtHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesWhtHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesWhtHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesWhtHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesWhtHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesWhtHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesWhtGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesWhtGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesWhtDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesWhtDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesWhtElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesWhtElfFemale,
                    _ => null
                },
            EyeGlowOrange =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesOrgHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesOrgHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesOrgHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesOrgHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesOrgHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesOrgHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesOrgGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesOrgGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesOrgDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesOrgDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesOrgElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesOrgElfFemale,
                    _ => null
                },
            EyeGlowPurple =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesPurHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesPurHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesPurHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesPurHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesPurHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesPurHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesPurGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesPurGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesPurDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesPurDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesPurElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesPurElfFemale,
                    _ => null
                },
            EyeGlowRedFlame =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesRedFlameHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => VfxType.EyesRedFlameHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => VfxType.EyesRedFlameHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => VfxType.EyesRedFlameHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => VfxType.EyesRedFlameHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => VfxType.EyesRedFlameHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => VfxType.EyesRedFlameGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => VfxType.EyesRedFlameGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => VfxType.EyesRedFlameDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => VfxType.EyesRedFlameDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => VfxType.EyesRedFlameElfMale,
                    (Gender.Female, AppearanceType.Elf) => VfxType.EyesRedFlameElfFemale,
                    _ => null
                },
            EyeGlowBlue =>
                (monk.Gender, appearanceType) switch
                {
                    (Gender.Male, AppearanceType.Human or AppearanceType.HalfElf) => EyesBlueHumanMale,
                    (Gender.Female, AppearanceType.Human or AppearanceType.HalfElf) => EyesBlueHumanFemale,
                    (Gender.Male, AppearanceType.HalfOrc) => EyesBlueHalforcMale,
                    (Gender.Female, AppearanceType.HalfOrc) => EyesBlueHalforcFemale,
                    (Gender.Male, AppearanceType.Halfling) => EyesBlueHalflingMale,
                    (Gender.Female, AppearanceType.Halfling) => EyesBlueHalflingFemale,
                    (Gender.Male, AppearanceType.Gnome) => EyesBlueGnomeMale,
                    (Gender.Female, AppearanceType.Gnome) => EyesBlueGnomeFemale,
                    (Gender.Male, AppearanceType.Dwarf) => EyesBlueDwarfMale,
                    (Gender.Female, AppearanceType.Dwarf) => EyesBlueDwarfFemale,
                    (Gender.Male, AppearanceType.Elf) => EyesBlueElfMale,
                    (Gender.Female, AppearanceType.Elf) => EyesBlueElfFemale,
                    _ => null
                },
            _ => null
        };
    }
}
