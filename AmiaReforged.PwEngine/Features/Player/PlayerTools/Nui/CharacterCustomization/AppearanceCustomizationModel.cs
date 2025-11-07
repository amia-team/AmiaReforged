using Anvil.API;
using Newtonsoft.Json;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class AppearanceCustomizationModel(NwPlayer player)
{
    private const string BackupDataKey = "APPEARANCE_CUSTOMIZATION_BACKUP";

    // Blocked heads per race/gender combination
    private static readonly Dictionary<string, HashSet<int>> BlockedHeads = new()
    {
        // Female Dwarf
        ["pfd"] = [156],

        // Male Dwarf
        ["pmd"] = [138, 156],

        // Female Elf
        ["pfe"] = [27, 112, 120, 121, 122, 154, 192, 196],

        // Male Elf
        ["pme"] = [23, 30, 103, 104, 119, 120, 121, 122, 123, 181, 189, 190, 191, 192, 193],

        // Female Halfling/Gnome
        ["pfa"] = [32, 54, 168, 194],

        // Male Halfling/Gnome
        ["pma"] = [33, 34, 72, 168, 181, 189, 190, 191, 192, 193],

        // Female Half-Orc
        ["pfo"] = [13, 151, 152, 133, 134],

        // Male Half-Orc
        ["pmo"] = [34, 35, 130, 132, 133, 134, 135, 138, 151, 152, 153, 154, 155, 158, 159, 194, 198, 199],

        // Female Human/Half-elf
        ["pfh"] = [39, 40, 41, 55, 57, 58, 175, 214],

        // Male Human/Half-elf
        ["pmh"] =
        [
            48, 49, 51, 62, 114, 115, 116, 117, 118, 119, 120, 121, 123, 128, 133, 134, 135, 136, 137, 138, 139, 178, 179, 180, 181,
            182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199, 222, 236, 240
        ]
    };

    // Additional blocked heads for Phenotype 2 only (these are in addition to the primary blocks above)
    private static readonly Dictionary<string, HashSet<int>> Phenotype2BlockedHeads = new()
    {
        ["pfd"] = [],
        ["pmd"] = [],
        ["pfe"] = [141],
        ["pme"] = [],
        ["pfa"] = [],
        ["pma"] = [],
        ["pfo"] = [],
        ["pmo"] = [],
        ["pfh"] = [144],
        ["pmh"] = [124, 129, 130, 131, 132, 143, 145, 146, 149, 150, 162]
    };

    // Additional blocked heads for Phenotype 0 only (these are in addition to the primary blocks above)
    private static readonly Dictionary<string, HashSet<int>> Phenotype0BlockedHeads = new()
    {
        // Only Male Human has phenotype 0 specific blocks so far
        ["pmh"] = [50, 53, 63]
    };

    public int HeadModel { get; private set; } = 1;
    private int _headModelMax = 255;

    public float Scale { get; private set; } = 1.0f;
    private const float MinScale = 0.9f;  // 90%
    private const float MaxScale = 1.1f;  // 110%

    public int CurrentSoundset { get; private set; }
    public string CurrentSoundsetResRef { get; private set; } = "";
    public int NewSoundset { get; private set; }
    public string NewSoundsetResRef { get; private set; } = "";

    public string CurrentPortrait { get; private set; } = "";
    public string NewPortrait { get; private set; } = "";

    // Tattoo properties
    private int CurrentTattooPart { get; set; } = 0; // Index into TattooParts array (Left Bicep is default)
    public readonly int[] TattooModels = new int[9]; // Model for each tattoo part (public so Presenter can read for display)
    private static readonly int[] TattooParts =
    [
        NWScript.CREATURE_PART_LEFT_BICEP,
        NWScript.CREATURE_PART_LEFT_FOREARM,
        NWScript.CREATURE_PART_LEFT_SHIN,
        NWScript.CREATURE_PART_LEFT_THIGH,
        NWScript.CREATURE_PART_RIGHT_BICEP,
        NWScript.CREATURE_PART_RIGHT_FOREARM,
        NWScript.CREATURE_PART_RIGHT_SHIN,
        NWScript.CREATURE_PART_RIGHT_THIGH,
        NWScript.CREATURE_PART_TORSO
    ];
    private static readonly string[] TattooPartNames =
    [
        "Left Bicep",
        "Left Forearm",
        "Left Shin",
        "Left Thigh",
        "Right Bicep",
        "Right Forearm",
        "Right Shin",
        "Right Thigh",
        "Torso"
    ];

    // Color channel properties (0 = Tattoo1, 1 = Tattoo2, 2 = Hair)
    public int CurrentColorChannel { get; private set; } = 0;
    public int Tattoo1Color { get; private set; } = 0;
    public int Tattoo2Color { get; private set; } = 0;
    public int HairColor { get; private set; } = 0;

    public void LoadInitialValues()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Load head model
        HeadModel = NWScript.GetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, creature);

        // Load scale
        VisualTransform transform = creature.VisualTransform;
        Scale = (float)Math.Round(transform.Scale, 2);

        // Load soundset
        CurrentSoundset = creature.SoundSet;
        CurrentSoundsetResRef = $"soundset_{CurrentSoundset}";

        // Load portrait
        CurrentPortrait = creature.PortraitResRef;
        NewPortrait = CurrentPortrait;

        // Load tattoo models for all parts
        for (int i = 0; i < TattooParts.Length; i++)
        {
            TattooModels[i] = NWScript.GetCreatureBodyPart(TattooParts[i], creature);
        }

        // Load tattoo and hair colors
        Tattoo1Color = NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_1);
        Tattoo2Color = NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_2);
        HairColor = NWScript.GetColor(creature, NWScript.COLOR_CHANNEL_HAIR);

        // Save ALL initial values as restore point
        SaveBackupToPcKey();
    }

    public void LoadVoicesetAndPortrait()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Load current soundset
        CurrentSoundset = creature.SoundSet;
        // Get resref from soundset.2da if possible (row index)
        CurrentSoundsetResRef = $"soundset_{CurrentSoundset}";

        // Load current portrait
        CurrentPortrait = creature.PortraitResRef;
        NewPortrait = CurrentPortrait;
    }

    public void SetVoiceset(string input)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        if (int.TryParse(input, out int soundsetId))
        {
            creature.SoundSet = (ushort)soundsetId;
            CurrentSoundset = soundsetId;
            NewSoundset = soundsetId;
            CurrentSoundsetResRef = $"soundset_{soundsetId}";
            NewSoundsetResRef = $"soundset_{soundsetId}";
            player.SendServerMessage($"Voiceset changed to #{soundsetId}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Please use the voiceset line number. You can find the full list of voicesets on the forums.", ColorConstants.Orange);
        }
    }

    public void SetPortrait(string resref)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        if (string.IsNullOrWhiteSpace(resref))
        {
            player.SendServerMessage("Please enter a portrait resref.", ColorConstants.Orange);
            return;
        }

        creature.PortraitResRef = resref;
        CurrentPortrait = resref;
        NewPortrait = resref;
        player.SendServerMessage($"Portrait changed to {resref}.", ColorConstants.Green);
    }

    public void SelectColorChannel(int channel)
    {
        CurrentColorChannel = channel;
        string channelName = channel switch
        {
            0 => "Tattoo 1",
            1 => "Tattoo 2",
            2 => "Hair",
            _ => "Unknown"
        };
        player.SendServerMessage($"Selected {channelName} color channel.", ColorConstants.Cyan);
    }

    public void SetColor(int colorIndex)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        switch (CurrentColorChannel)
        {
            case 0: // Tattoo 1
                Tattoo1Color = colorIndex;
                NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_1, colorIndex);
                player.SendServerMessage($"Tattoo 1 color set to {colorIndex}.", ColorConstants.Green);
                break;
            case 1: // Tattoo 2
                Tattoo2Color = colorIndex;
                NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_2, colorIndex);
                player.SendServerMessage($"Tattoo 2 color set to {colorIndex}.", ColorConstants.Green);
                break;
            case 2: // Hair
                HairColor = colorIndex;
                NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_HAIR, colorIndex);
                player.SendServerMessage($"Hair color set to {colorIndex}.", ColorConstants.Green);
                break;
        }
    }

    public void SelectTattoo()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Load current tattoo models for all parts
        for (int i = 0; i < TattooParts.Length; i++)
        {
            TattooModels[i] = NWScript.GetCreatureBodyPart(TattooParts[i], creature);
        }

        CurrentTattooPart = 0; // Default to Left Bicep
        player.SendServerMessage("Selected tattoos. Use part selector to choose which body part to customize.", ColorConstants.Cyan);
    }

    public string GetCurrentTattooPartName()
    {
        return TattooPartNames[CurrentTattooPart];
    }

    public int GetCurrentTattooPartIndex()
    {
        return CurrentTattooPart;
    }

    public int GetCurrentTattooModel()
    {
        return TattooModels[CurrentTattooPart];
    }

    public void CycleTattooPart(int direction)
    {
        CurrentTattooPart += direction;

        if (CurrentTattooPart < 0)
            CurrentTattooPart = TattooParts.Length - 1;
        else if (CurrentTattooPart >= TattooParts.Length)
            CurrentTattooPart = 0;

        player.SendServerMessage($"Selected {TattooPartNames[CurrentTattooPart]}. Current model: {TattooModels[CurrentTattooPart]}", ColorConstants.Cyan);
    }

    public void AdjustTattooModel(int delta)
    {
        int currentModel = TattooModels[CurrentTattooPart];

        // Only cycle between models 1 and 2 (all races only have 2 tattoo options for now)
        int newModel;
        if (delta > 0) // Going forward
        {
            newModel = currentModel == 1 ? 2 : 1;
        }
        else // Going backward
        {
            newModel = currentModel == 2 ? 1 : 2;
        }

        TattooModels[CurrentTattooPart] = newModel;
        ApplyTattooChanges();

        player.SendServerMessage($"{TattooPartNames[CurrentTattooPart]} tattoo model set to {newModel}.", ColorConstants.Green);
    }

    private void ApplyTattooChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        int partType = TattooParts[CurrentTattooPart];
        int model = TattooModels[CurrentTattooPart];

        NWScript.SetCreatureBodyPart(partType, model, creature);
    }

    public void SelectHead()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Get current head model (CREATURE_PART_HEAD = 0)
        HeadModel = NWScript.GetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, creature);

        // Get max head model for this race/gender - for now use a reasonable max
        _headModelMax = 255;

        player.SendServerMessage($"Selected head. Current model: {HeadModel}", ColorConstants.Cyan);
    }

    public void SelectScale()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Get current scale from visual transform
        VisualTransform transform = creature.VisualTransform;
        Scale = (float)Math.Round(transform.Scale, 2);

        player.SendServerMessage($"Selected scale. Current value: {(int)Math.Round(Scale * 100)}%", ColorConstants.Cyan);
    }

    public void AdjustScale(int percentDelta)
    {
        float delta = percentDelta / 100f;
        float newScale = Scale + delta;

        // Round to 2 decimal places to avoid floating-point precision issues
        newScale = (float)Math.Round(newScale, 2);

        // Check limits and give feedback
        if (newScale < MinScale)
        {
            player.SendServerMessage($"Character cannot be scaled lower than {(int)(MinScale * 100)}%.", ColorConstants.Orange);
            return;
        }

        if (newScale > MaxScale)
        {
            player.SendServerMessage($"Character cannot be scaled higher than {(int)(MaxScale * 100)}%.", ColorConstants.Orange);
            return;
        }

        Scale = newScale;
        ApplyScaleChanges();

        int scalePercent = (int)Math.Round(Scale * 100);
        player.SendServerMessage($"Character scale set to {scalePercent}%.", ColorConstants.Green);
    }

    private void ApplyScaleChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, Scale);

        // Read back the actual scale from the creature to stay in sync
        VisualTransform transform = creature.VisualTransform;
        Scale = (float)Math.Round(transform.Scale, 2);
    }

    public void AdjustHeadModel(int delta)
    {
        HeadModel = GetNextValidHeadModel(HeadModel, delta, _headModelMax);
        ApplyHeadChanges();
        player.SendServerMessage($"Head model set to {HeadModel}.", ColorConstants.Green);
    }

    public void SetHeadModelDirect(int modelNumber)
    {
        if (modelNumber < 1 || modelNumber > _headModelMax)
        {
            player.SendServerMessage($"Head model must be between 1 and {_headModelMax}.", ColorConstants.Orange);
            return;
        }

        if (IsValidHeadModel(modelNumber))
        {
            HeadModel = modelNumber;
            ApplyHeadChanges();
            player.SendServerMessage($"Head model set to {HeadModel}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage($"Head model {modelNumber} is not valid for your race/gender.", ColorConstants.Orange);
        }
    }

    private int GetNextValidHeadModel(int currentModel, int delta, int maxModel)
    {
        if (maxModel <= 0) return currentModel;

        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchModel = currentModel;
        int attemptsRemaining = maxModel + 1;

        while (attemptsRemaining > 0)
        {
            if (step >= 10)
            {
                searchModel += delta;
                step = 1;
            }
            else
            {
                searchModel += direction;
            }

            if (searchModel > maxModel)
            {
                searchModel = 1;
            }
            else if (searchModel < 1)
            {
                searchModel = maxModel;
            }

            if (searchModel == currentModel && attemptsRemaining < maxModel)
            {
                player.SendServerMessage("No other valid head models found.", ColorConstants.Orange);
                return currentModel;
            }

            if (IsValidHeadModel(searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid head model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidHeadModel(int modelNumber)
    {
        if (modelNumber < 1) return false;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return false;

        string prefix = GetHeadPrefix(creature);
        int phenotype = GetPhenotype(creature);

        // Check if head is in the primary blocked list for this race/gender
        if (BlockedHeads.TryGetValue(prefix, out HashSet<int>? blockedSet))
        {
            if (blockedSet.Contains(modelNumber))
            {
                player.SendServerMessage($"Head {modelNumber} is blocked for {prefix}", ColorConstants.Gray);
                return false;
            }
        }

        // Check if head is in the phenotype 0 blocked list (if character is phenotype 0)
        if (phenotype == 0 && Phenotype0BlockedHeads.TryGetValue(prefix, out HashSet<int>? phenotype0BlockedSet))
        {
            if (phenotype0BlockedSet.Contains(modelNumber))
            {
                player.SendServerMessage($"Head {modelNumber} is blocked for {prefix} phenotype 0", ColorConstants.Gray);
                return false;
            }
        }

        // Check if head is in the phenotype 2 blocked list (if character is phenotype 2)
        if (phenotype == 2 && Phenotype2BlockedHeads.TryGetValue(prefix, out HashSet<int>? phenotype2BlockedSet))
        {
            if (phenotype2BlockedSet.Contains(modelNumber))
            {
                player.SendServerMessage($"Head {modelNumber} is blocked for {prefix} phenotype 2", ColorConstants.Gray);
                return false;
            }
        }

        // Check if the head model file exists using ResourceManager
        string modelResRef = $"{prefix}{phenotype}_head{modelNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);

        bool isValid = !string.IsNullOrEmpty(alias);
        player.SendServerMessage($"Testing head: {modelResRef} (MDL) -> alias: '{alias}' -> valid: {isValid}", ColorConstants.Gray);

        return isValid;
    }

    private string GetHeadPrefix(NwCreature creature)
    {
        string genderLetter = creature.Gender == Gender.Female ? "f" : "m";

        // Get race letter from appearance.2da instead of racial type
        // This handles cases where creatures use different appearance models (e.g., elf using human model)
        // Also handles mounted appearances (482-495)
        int appearanceId = creature.Appearance.RowIndex;

        string raceLetter = appearanceId switch
        {
            0 => "d",    // Dwarf
            1 => "e",    // Elf
            2 => "a",    // Gnome
            3 => "a",    // Halfling
            4 => "h",    // Half-Elf
            5 => "o",    // Half-Orc
            6 => "h",    // Human
            482 => "d",  // Dwarf (mounted)
            483 => "d",  // Dwarf (mounted)
            484 => "e",  // Elf (mounted)
            485 => "e",  // Elf (mounted)
            486 => "a",  // Gnome (mounted)
            487 => "a",  // Gnome (mounted)
            488 => "a",  // Halfling (mounted)
            489 => "a",  // Halfling (mounted)
            490 => "h",  // Half-Elf (mounted)
            491 => "h",  // Half-Elf (mounted)
            492 => "o",  // Half-Orc (mounted)
            493 => "o",  // Half-Orc (mounted)
            494 => "h",  // Human (mounted)
            495 => "h",  // Human (mounted)
            _ => "h"     // Default to human for unknown appearances
        };

        return $"p{genderLetter}{raceLetter}";
    }

    private int GetPhenotype(NwCreature creature)
    {
        int phenotype = (int)creature.Phenotype;
        // Only 0 or 2 are valid, default to 0 if not 2
        return phenotype == 2 ? 2 : 0;
    }

    private void ApplyHeadChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // CREATURE_PART_HEAD = 0
        NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, HeadModel, creature);
    }

    public void ApplyChanges()
    {
        SaveBackupToPcKey();
        player.SendServerMessage("Appearance customization saved! You can continue editing or click Discard to return to this save point.", ColorConstants.Green);
    }

    public void RevertChanges()
    {
        var backupData = LoadBackupFromPcKey();
        if (backupData == null)
        {
            player.SendServerMessage("No changes to revert.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Revert Head
        if (backupData.HeadModel.HasValue)
        {
            NWScript.SetCreatureBodyPart(NWScript.CREATURE_PART_HEAD, backupData.HeadModel.Value, creature);
            HeadModel = backupData.HeadModel.Value;
            player.SendServerMessage("Head appearance reverted to last save point.", ColorConstants.Cyan);
        }

        // Revert Scale
        if (backupData.Scale.HasValue)
        {
            Scale = backupData.Scale.Value;
            NWScript.SetObjectVisualTransform(creature, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, Scale);
            player.SendServerMessage("Character scale reverted to last save point.", ColorConstants.Cyan);
        }

        // Revert Soundset
        if (backupData.Soundset.HasValue)
        {
            creature.SoundSet = (ushort)backupData.Soundset.Value;
            CurrentSoundset = backupData.Soundset.Value;
            NewSoundset = backupData.Soundset.Value;
            player.SendServerMessage("Voiceset reverted to last save point.", ColorConstants.Cyan);
        }

        // Revert Portrait
        if (!string.IsNullOrEmpty(backupData.Portrait))
        {
            creature.PortraitResRef = backupData.Portrait;
            CurrentPortrait = backupData.Portrait;
            NewPortrait = backupData.Portrait;
            player.SendServerMessage("Portrait reverted to last save point.", ColorConstants.Cyan);
        }

        // Revert Tattoos
        if (backupData.TattooModels != null && backupData.TattooModels.Length == TattooParts.Length)
        {
            for (int i = 0; i < TattooParts.Length; i++)
            {
                TattooModels[i] = backupData.TattooModels[i];
                NWScript.SetCreatureBodyPart(TattooParts[i], TattooModels[i], creature);
            }
            player.SendServerMessage("Tattoos reverted to last save point.", ColorConstants.Cyan);
        }

        // Revert Colors
        if (backupData.Tattoo1Color.HasValue)
        {
            Tattoo1Color = backupData.Tattoo1Color.Value;
            NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_1, Tattoo1Color);
        }
        if (backupData.Tattoo2Color.HasValue)
        {
            Tattoo2Color = backupData.Tattoo2Color.Value;
            NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_TATTOO_2, Tattoo2Color);
        }
        if (backupData.HairColor.HasValue)
        {
            HairColor = backupData.HairColor.Value;
            NWScript.SetColor(creature, NWScript.COLOR_CHANNEL_HAIR, HairColor);
        }
        if (backupData.Tattoo1Color.HasValue || backupData.Tattoo2Color.HasValue || backupData.HairColor.HasValue)
        {
            player.SendServerMessage("Colors reverted to last save point.", ColorConstants.Cyan);
        }
    }

    public void ConfirmAndClose()
    {
        ClearBackupFromPcKey();
        player.SendServerMessage("Appearance customization confirmed!", ColorConstants.Green);
    }

    private void SaveBackupToPcKey()
    {
        var backupData = new AppearanceBackupData
        {
            HeadModel = HeadModel,
            Scale = Scale,
            Soundset = CurrentSoundset,
            Portrait = CurrentPortrait,
            TattooModels = (int[])TattooModels.Clone(),
            Tattoo1Color = Tattoo1Color,
            Tattoo2Color = Tattoo2Color,
            HairColor = HairColor
        };

        string json = JsonConvert.SerializeObject(backupData);

        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.SetLocalString(pcKey, BackupDataKey, json);
        }
    }

    private AppearanceBackupData? LoadBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid)
            return null;

        string json = NWScript.GetLocalString(pcKey, BackupDataKey);
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<AppearanceBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private void ClearBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.DeleteLocalString(pcKey, BackupDataKey);
        }
    }
}

public class AppearanceBackupData
{
    public int? HeadModel { get; set; }
    public float? Scale { get; set; }
    public int? Soundset { get; set; }
    public string? Portrait { get; set; }
    public int[]? TattooModels { get; set; }
    public int? Tattoo1Color { get; set; }
    public int? Tattoo2Color { get; set; }
    public int? HairColor { get; set; }
}

