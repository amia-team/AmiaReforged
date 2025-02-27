using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MonkDialogHandler))]
public class MonkDialogHandler
{ 
    private readonly Logger Log = LogManager.GetCurrentClassLogger();

    [Inject] private DialogService DialogService { get; init; }
    public MonkDialogHandler(DialogService dialogService)
    { 
      //Register method to listen for the event.
      DialogService = dialogService;
      NwModule.Instance.OnUseFeat += OpenPathDialog;
      NwModule.Instance.OnUseFeat += OpenEyeGlowDialog;
      Log.Info("Monk Eye Glow Feat Handler initialized.");
    }

    /// <summary>
    /// Opens the dialog menu for choosing the Path of Enlightenment
    /// </summary>
    private static async void OpenPathDialog(OnUseFeat eventData)
    {
    if (eventData.Feat.Id is not MonkFeat.PathOfEnlightenment) return;
    if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

    if (eventData.Creature.Feats.Any(feat => feat.Id is MonkFeat.CrashingMeteor
          or MonkFeat.SwingingCenser or MonkFeat.CrystalTides or MonkFeat.ChardalynSand
          or MonkFeat.IroncladBull or MonkFeat.CrackedVessel or MonkFeat.EchoingValley)) return;

    await player.ActionStartConversation
        (eventData.Creature, "mont_path_dlg", true, false);
    }

    /// <summary>
    /// Opens the dialog menu to set the eye glow color
    /// </summary>
    private static async void OpenEyeGlowDialog(OnUseFeat eventData)
    {
    if (eventData.Feat.FeatType is not Feat.PerfectSelf) return; 
    if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

    await player.ActionStartConversation
        (eventData.Creature, "monk_eye_dlg", true, false);
    }

    [ScriptHandler("monk_path_dlg")]
    private void PathDialog(CallInfo info)
    {
        DialogEvents.AppearsWhen eventData = new();
        
        if (eventData.PlayerSpeaker?.ControlledCreature is null) return;
        
        NwCreature monk = eventData.PlayerSpeaker.ControlledCreature;
        NodeType nodeType = DialogService.CurrentNodeType;

        if (nodeType == NodeType.ReplyNode)
        {
            string path = GivePathFeat(monk);
            eventData.PlayerSpeaker.SendServerMessage($"{path} added.", MonkColors.MonkColorScheme);
        }
    }

    [ScriptHandler("monk_eye_dlg")]
    private void EyeGlowDialog(CallInfo info)
    {
        DialogEvents.AppearsWhen eventData = new();

        if (eventData.PlayerSpeaker?.ControlledCreature is null) return;
        
        NwCreature monk = eventData.PlayerSpeaker.ControlledCreature;
        NodeType nodeType = DialogService.CurrentNodeType;
        

        if (nodeType == NodeType.StartingNode)
        {
            DialogService.SetCurrentNodeText("Select eye glow color.");
        }
        if (nodeType == NodeType.ReplyNode)
        {
            ApplyEyeGlow(monk);
        }
    }
    
    /// <summary>
    /// Selects the monk path based on dialog option
    /// </summary>
    /// <returns>Path name</returns>
    private static string GivePathFeat(NwCreature monk)
    {
        Func<string, LocalVariableInt> localInt = monk.GetObjectVariable<LocalVariableInt>;

        if (localInt("ds_check1").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.CrystalTides)!);
            return NwFeat.FromFeatId(MonkFeat.CrystalTides)!.Name.ToString();
        }


        if (localInt("ds_check2").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.EchoingValley)!);
            return NwFeat.FromFeatId(MonkFeat.EchoingValley)!.Name.ToString();
        }


        if (localInt("ds_check3").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.CrackedVessel)!);
            return NwFeat.FromFeatId(MonkFeat.CrackedVessel)!.Name.ToString();
        }


        if (localInt("ds_check4").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.CrashingMeteor)!);
            return NwFeat.FromFeatId(MonkFeat.CrashingMeteor)!.Name.ToString();
        }


        if (localInt("ds_check5").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.IroncladBull)!);
            return NwFeat.FromFeatId(MonkFeat.IroncladBull)!.Name.ToString();
        }

        if (localInt("ds_check6").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.SwingingCenser)!);
            return NwFeat.FromFeatId(MonkFeat.SwingingCenser)!.Name.ToString();
        }

        if (localInt("ds_check7").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.ChardalynSand)!);
            return NwFeat.FromFeatId(MonkFeat.ChardalynSand)!.Name.ToString();
        }
        
        return "";
    }

    private static void ApplyEyeGlow(NwCreature monk)
    {
        Effect monkEyeVfx = GetMonkEyeVfx(monk);
        monkEyeVfx.SubType = EffectSubType.Unyielding;
        monkEyeVfx.Tag = "monk_eye_glow_vfx";
        monk.ApplyEffect(EffectDuration.Permanent, monkEyeVfx);
    }

    /// <summary>
    /// Helper that returns the vfx effect for monk eye glow
    /// </summary>
    private static Effect GetMonkEyeVfx(NwCreature monk)
    {
        VfxType eyeGlowVfx = VfxType.None;
        Func<string, LocalVariableInt> localInt = monk.GetObjectVariable<LocalVariableInt>;
        AppearanceTableEntry appearanceType = monk.Appearance;
        Gender gender = monk.Gender;
        float scale = monk.VisualTransform.Scale;
        
        // CYAN
        if (localInt("ds_check1").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesCynHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesCynHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesCynHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesCynHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesCynHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesCynHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesCynGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesCynGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesCynDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesCynDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesCynElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesCynElfFemale,
                _ => eyeGlowVfx
            };
        // GREEN
        if (localInt("ds_check2").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesGreenHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesGreenHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesGreenHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesGreenHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesGreenHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesGreenHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesGreenGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesGreenGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesGreenDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesGreenDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesGreenElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesGreenElfFemale,
                _ => eyeGlowVfx
            };
        // YELLOW
        if (localInt("ds_check3").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesYelHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesYelHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesYelHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesYelHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesYelHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesYelHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesYelGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesYelGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesYelDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesYelDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesYelElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesYelElfFemale,
                _ => eyeGlowVfx
            };
        // WHITE
        if (localInt("ds_check4").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesWhtHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesWhtHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesWhtHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesWhtHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesWhtHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesWhtHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesWhtGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesWhtGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesWhtDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesWhtDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesWhtElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesWhtElfFemale,
                _ => eyeGlowVfx
            };
        // ORANGE
        if (localInt("ds_check5").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesOrgHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesOrgHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesOrgHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesOrgHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesOrgHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesOrgHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesOrgGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesOrgGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesOrgDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesOrgDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesOrgElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesOrgElfFemale,
                _ => eyeGlowVfx
            };
        // PURPLE
        if (localInt("ds_check6").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesPurHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesPurHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesPurHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesPurHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesPurHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesPurHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesPurGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesPurGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesPurDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesPurDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesPurElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesPurElfFemale,
                _ => eyeGlowVfx
            };
        // RED FLAME
        if (localInt("ds_check7").HasValue) 
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesRedFlameHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesRedFlameHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => VfxType.EyesRedFlameHalforcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => VfxType.EyesRedFlameHumanFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => VfxType.EyesRedFlameHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => VfxType.EyesRedFlameHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => VfxType.EyesRedFlameGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => VfxType.EyesRedFlameGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => VfxType.EyesRedFlameDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => VfxType.EyesRedFlameDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => VfxType.EyesRedFlameElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => VfxType.EyesRedFlameElfFemale,
                _ => eyeGlowVfx
            };
        // BLUE
        
        const VfxType eyesBlueHumanMale = (VfxType)324;
        const VfxType eyesBlueHumanFemale = (VfxType)325;
        const VfxType eyesBlueDwarfMale = (VfxType)326;
        const VfxType eyesBlueDwarfFemale = (VfxType)327;
        const VfxType eyesBlueElfMale = (VfxType)328;
        const VfxType eyesBlueElfFemale = (VfxType)329;
        const VfxType eyesBlueGnomeMale = (VfxType)330;
        const VfxType eyesBlueGnomeFemale = (VfxType)331;
        const VfxType eyesBlueHalflingMale = (VfxType)332;
        const VfxType eyesBlueHalflingFemale = (VfxType)333;
        const VfxType eyesBlueHalfOrcMale = (VfxType)334;
        const VfxType eyesBlueHalfOrcFemale = (VfxType)335;
        
        if (localInt("ds_check8").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => eyesBlueHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => eyesBlueHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => eyesBlueHalfOrcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => eyesBlueHalfOrcFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => eyesBlueHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => eyesBlueHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => eyesBlueGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => eyesBlueGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => eyesBlueDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => eyesBlueDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => eyesBlueElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => eyesBlueElfFemale,
                _ => eyeGlowVfx
            };
        
        // REMOVE AND RETURN
        if (localInt("ds_check9").HasValue)
        {
            foreach (Effect effect in monk.ActiveEffects)
            {
                if (effect.Tag is "monk_eye_glow_vfx") 
                    monk.RemoveEffect(effect);
            }
        }
        
        return Effect.VisualEffect(eyeGlowVfx, false, scale);
    }
}