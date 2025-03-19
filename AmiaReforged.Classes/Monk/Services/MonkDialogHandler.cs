using AmiaReforged.Classes.Monk.Constants;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(MonkDialogHandler))]
public class MonkDialogHandler
{
    private readonly Logger _log = LogManager.GetCurrentClassLogger();

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
    private const VfxType EyesBlueHalfOrcMale = (VfxType)334;
    private const VfxType EyesBlueHalfOrcFemale = (VfxType)335;
    
    

    public MonkDialogHandler(DialogService dialogService)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        //Register method to listen for the event.
        DialogService = dialogService;
        NwModule.Instance.OnUseFeat += OpenPathDialog;
        NwModule.Instance.OnUseFeat += OpenEyeGlowDialog;
        NwModule.Instance.OnUseFeat += OpenFightingStyleDialog;
        _log.Info(message: "Monk Eye Glow Feat Handler initialized.");
    }

    [Inject] private DialogService DialogService { get; init; }

    /// <summary>
    ///     Opens the dialog menu for choosing the Path of Enlightenment
    /// </summary>
    private static async void OpenPathDialog(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.PathOfEnlightenment) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (eventData.Creature.Feats.Any(feat => feat.Id is MonkFeat.CrashingMeteor
                or MonkFeat.SwingingCenser or MonkFeat.HiddenSpring or MonkFeat.FickleStrand
                or MonkFeat.IroncladBull or MonkFeat.CrackedVessel or MonkFeat.EchoingValley)) return;

        await player.ActionStartConversation
            (eventData.Creature, dialogResRef: "mont_path", true, false);
    }

    /// <summary>
    ///     Opens the dialog menu to set the eye glow
    /// </summary>
    private static async void OpenEyeGlowDialog(OnUseFeat eventData)
    {
        if (eventData.Feat.FeatType is not Feat.PerfectSelf) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        await player.ActionStartConversation
            (eventData.Creature, dialogResRef: "monk_eyeglow", true, false);
    }
    
    /// <summary>
    ///     Opens the dialog menu to choose the fighting style
    /// </summary>
    private async void OpenFightingStyleDialog(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.MonkFightingStyle) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        await player.ActionStartConversation
            (eventData.Creature, dialogResRef: "monk_fightingstyle", true, false);
    }
    
    

    [ScriptHandler(scriptName: "monk_path")]
    private void PathDialog(CallInfo info)
    {
        DialogEvents.AppearsWhen eventData = new();

        if (eventData.PlayerSpeaker?.ControlledCreature is null) return;

        NwCreature monk = eventData.PlayerSpeaker.ControlledCreature;
        NodeType nodeType = DialogService.CurrentNodeType;
        
        if (nodeType == NodeType.StartingNode) DialogService.SetCurrentNodeText(text: "Select Path of Enlightenment:");
        if (nodeType == NodeType.ReplyNode)
        {
            string path = GivePathFeat(monk);
            eventData.PlayerSpeaker.SendServerMessage($"{path} added.");
        }
    }

    [ScriptHandler(scriptName: "monk_eyeglow")]
    private void EyeGlowDialog(CallInfo info)
    {
        DialogEvents.AppearsWhen eventData = new();

        if (eventData.PlayerSpeaker?.ControlledCreature is null) return;

        NwCreature monk = eventData.PlayerSpeaker.ControlledCreature;
        NodeType nodeType = DialogService.CurrentNodeType;


        if (nodeType == NodeType.StartingNode) DialogService.SetCurrentNodeText(text: "Select eye glow:");
        if (nodeType == NodeType.ReplyNode) ApplyEyeGlow(monk);
    }
    
    [ScriptHandler(scriptName: "monk_fightingstyle")]
    private void FightingStyleDialog(CallInfo info)
    {
        DialogEvents.AppearsWhen eventData = new();

        if (eventData.PlayerSpeaker?.ControlledCreature is null) return;

        NwCreature monk = eventData.PlayerSpeaker.ControlledCreature;
        NodeType nodeType = DialogService.CurrentNodeType;


        if (nodeType == NodeType.StartingNode) DialogService.SetCurrentNodeText(text: "Select fighting style:");
        if (nodeType == NodeType.ReplyNode)
        {
            string addedFeats = GiveFightingStyleFeats(monk);
            eventData.PlayerSpeaker.SendServerMessage($"{addedFeats} added.");
        }
    }

    /// <summary>
    ///     Selects the monk path based on dialog option
    /// </summary>
    /// <returns>Path name</returns>
    private static string GivePathFeat(NwCreature monk)
    {
        Func<string, LocalVariableInt> localInt = monk.GetObjectVariable<LocalVariableInt>;

        if (localInt(arg: "ds_check1").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.HiddenSpring)!);
            return NwFeat.FromFeatId(MonkFeat.HiddenSpring)!.Name.ToString();
        }


        if (localInt(arg: "ds_check2").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.EchoingValley)!);
            return NwFeat.FromFeatId(MonkFeat.EchoingValley)!.Name.ToString();
        }


        if (localInt(arg: "ds_check3").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.CrackedVessel)!);
            return NwFeat.FromFeatId(MonkFeat.CrackedVessel)!.Name.ToString();
        }


        if (localInt(arg: "ds_check4").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.CrashingMeteor)!);
            return NwFeat.FromFeatId(MonkFeat.CrashingMeteor)!.Name.ToString();
        }


        if (localInt(arg: "ds_check5").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.IroncladBull)!);
            return NwFeat.FromFeatId(MonkFeat.IroncladBull)!.Name.ToString();
        }

        if (localInt(arg: "ds_check6").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.SwingingCenser)!);
            return NwFeat.FromFeatId(MonkFeat.SwingingCenser)!.Name.ToString();
        }

        if (localInt(arg: "ds_check7").HasValue)
        {
            monk.AddFeat(NwFeat.FromFeatId(MonkFeat.FickleStrand)!);
            return NwFeat.FromFeatId(MonkFeat.FickleStrand)!.Name.ToString();
        }

        return "";
    }

    private static void ApplyEyeGlow(NwCreature monk)
    {
        Effect monkEyeVfx = GetMonkEyeVfx(monk);
        monkEyeVfx.SubType = EffectSubType.Unyielding;
        monkEyeVfx.Tag = "monk_eyeglow";
        monk.ApplyEffect(EffectDuration.Permanent, monkEyeVfx);
    }

    /// <summary>
    ///     Helper that returns the vfx effect for monk eye glow
    /// </summary>
    private static Effect GetMonkEyeVfx(NwCreature monk)
    {
        VfxType eyeGlowVfx = VfxType.None;
        Func<string, LocalVariableInt> localInt = monk.GetObjectVariable<LocalVariableInt>;
        AppearanceTableEntry appearanceType = monk.Appearance;
        Gender gender = monk.Gender;
        float scale = monk.VisualTransform.Scale;

        // CYAN
        if (localInt(arg: "ds_check1").HasValue)
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
        if (localInt(arg: "ds_check2").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType.EyesGreenHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType
                    .EyesGreenHumanFemale,
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
        if (localInt(arg: "ds_check3").HasValue)
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
        if (localInt(arg: "ds_check4").HasValue)
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
        if (localInt(arg: "ds_check5").HasValue)
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
        if (localInt(arg: "ds_check6").HasValue)
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
        if (localInt(arg: "ds_check7").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) =>
                    VfxType.EyesRedFlameHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => VfxType
                    .EyesRedFlameHumanFemale,
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
        if (localInt(arg: "ds_check8").HasValue)
            eyeGlowVfx = (gender, appearanceType.RowIndex) switch
            {
                (Gender.Male, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => EyesBlueHumanMale,
                (Gender.Female, (int)AppearanceType.Human or (int)AppearanceType.HalfElf) => EyesBlueHumanFemale,
                (Gender.Male, (int)AppearanceType.HalfOrc) => EyesBlueHalfOrcMale,
                (Gender.Female, (int)AppearanceType.HalfOrc) => EyesBlueHalfOrcFemale,
                (Gender.Male, (int)AppearanceType.Halfling) => EyesBlueHalflingMale,
                (Gender.Female, (int)AppearanceType.Halfling) => EyesBlueHalflingFemale,
                (Gender.Male, (int)AppearanceType.Gnome) => EyesBlueGnomeMale,
                (Gender.Female, (int)AppearanceType.Gnome) => EyesBlueGnomeFemale,
                (Gender.Male, (int)AppearanceType.Dwarf) => EyesBlueDwarfMale,
                (Gender.Female, (int)AppearanceType.Dwarf) => EyesBlueDwarfFemale,
                (Gender.Male, (int)AppearanceType.Elf) => EyesBlueElfMale,
                (Gender.Female, (int)AppearanceType.Elf) => EyesBlueElfFemale,
                _ => eyeGlowVfx
            };

        // REMOVE AND RETURN
        if (localInt(arg: "ds_check9").HasValue)
            foreach (Effect effect in monk.ActiveEffects)
            {
                if (effect.Tag is "monk_eyeglow")
                    monk.RemoveEffect(effect);
            }

        return Effect.VisualEffect(eyeGlowVfx, false, scale);
    }
    
    /// <summary>
    ///  At level 6 monk, choose between IKD, Imp Disarm, and Called Shot and Mobility
    /// </summary>
    private static string GiveFightingStyleFeats(NwCreature monk)
    {
        Func<string, LocalVariableInt> localInt = monk.GetObjectVariable<LocalVariableInt>;
        
        // Improved Knockdown
        if (localInt(arg: "ds_check1").HasValue)
        {
            monk.AddFeat(Feat.ImprovedKnockdown!, 6);
            return NwFeat.FromFeatType(Feat.ImprovedKnockdown)!.Name.ToString();
        }


        if (localInt(arg: "ds_check2").HasValue)
        {
            monk.AddFeat(Feat.ImprovedDisarm!, 6);
            return NwFeat.FromFeatType(Feat.ImprovedDisarm)!.Name.ToString();
        }

        if (localInt(arg: "ds_check3").HasValue)
        {
            monk.AddFeat(Feat.Mobility!, 6);
            monk.AddFeat(Feat.CalledShot!, 6);
            return NwFeat.FromFeatType(Feat.Mobility)!.Name +" and "+ NwFeat.FromFeatType(Feat.CalledShot)!.Name;
        }

        return "";
    }
}