using Anvil.API;
using Anvil.Services;
using Newtonsoft.Json;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

public sealed class ThousandFacesModel(NwPlayer player, PlayerNameOverrideService playerNameOverrideService)
{
    private const string BackupDataKey = "THOUSAND_FACES_BACKUP";

    // Allowed racial types for appearance changes
    private static readonly HashSet<int> AllowedRacialTypes = [0, 1, 2, 3, 4, 5, 6, 8, 12, 13, 14, 15, 19, 25];

    public int HeadModel { get; private set; } = 1;
    private readonly int _headModelMax = 255;

    public int AppearanceType { get; private set; }
    private readonly int _appearanceTypeMax = 2118;

    public float Scale { get; private set; } = 1.0f;
    private const float MinScale = 0.4f;
    private const float MaxScale = 1.2f;

    public int CurrentSoundset { get; private set; }
    public string CurrentSoundsetResRef { get; private set; } = "";
    public int NewSoundset { get; private set; }
    public string NewSoundsetResRef { get; private set; } = "";

    public string CurrentPortrait { get; private set; } = "";
    public string NewPortrait { get; private set; } = "";

    // Color properties
    private int CurrentColorChannel { get; set; } = 1; // 0=Skin, 1=Hair, 2=Tattoo1, 3=Tattoo2 (default: Hair)
    public int SkinColor { get; private set; }
    public int HairColor { get; private set; }
    public int TattooColor1 { get; private set; }
    public int TattooColor2 { get; private set; }

    public void LoadInitialValues()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Save backup
        BackupData backup = new()
        {
            HeadModel = creature.GetCreatureBodyPart(CreaturePart.Head),
            AppearanceType = creature.Appearance.RowIndex,
            Scale = creature.VisualTransform.Scale,
            Soundset = creature.SoundSet,
            PortraitResRef = creature.PortraitResRef,
            SkinColor = creature.GetColor(ColorChannel.Skin),
            HairColor = creature.GetColor(ColorChannel.Hair),
            TattooColor1 = creature.GetColor(ColorChannel.Tattoo1),
            TattooColor2 = creature.GetColor(ColorChannel.Tattoo2)
        };

        string backupJson = JsonConvert.SerializeObject(backup);
        NWScript.SetLocalString(creature, BackupDataKey, backupJson);

        // Load current values
        HeadModel = backup.HeadModel;
        AppearanceType = backup.AppearanceType;
        Scale = backup.Scale;
        CurrentSoundset = backup.Soundset;
        CurrentSoundsetResRef = $"{backup.Soundset}";
        NewSoundset = backup.Soundset;
        NewSoundsetResRef = $"{backup.Soundset}";
        CurrentPortrait = backup.PortraitResRef;
        NewPortrait = backup.PortraitResRef;
        SkinColor = backup.SkinColor;
        HairColor = backup.HairColor;
        TattooColor1 = backup.TattooColor1;
        TattooColor2 = backup.TattooColor2;
    }

    public void SetHead(int newHead)
    {
        if (newHead < 1 || newHead > _headModelMax)
        {
            player.SendServerMessage($"Head model must be between 1 and {_headModelMax}.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Only validate if this is a manual set (not called from ModifyHead with search)
        if (!IsValidHeadModel(creature, newHead))
        {
            player.SendServerMessage($"Head model {newHead} does not exist for this race.", ColorConstants.Orange);
            return;
        }

        HeadModel = newHead;
        creature.SetCreatureBodyPart(CreaturePart.Head, newHead);
        player.SendServerMessage($"Head model changed to {newHead}.", ColorConstants.Green);
    }

    public void ModifyHead(int delta)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Search for the next valid head model
        int newHead = FindNextValidHead(creature, HeadModel, delta);

        if (newHead == HeadModel)
        {
            player.SendServerMessage("No other valid head models found.", ColorConstants.Orange);
            return;
        }

        HeadModel = newHead;
        creature.SetCreatureBodyPart(CreaturePart.Head, newHead);
        player.SendServerMessage($"Head model changed to {newHead}.", ColorConstants.Green);
    }

    private int FindNextValidHead(NwCreature creature, int currentHead, int delta)
    {
        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchHead = currentHead;
        int attemptsRemaining = _headModelMax + 1;

        while (attemptsRemaining > 0)
        {
            // Move by the delta amount (10 or 1)
            if (step >= 10)
            {
                searchHead += delta;
                step = 1; // After the first jump, search one by one
            }
            else
            {
                searchHead += direction;
            }

            // Wrap around
            if (searchHead > _headModelMax)
            {
                searchHead = 1;
            }
            else if (searchHead < 1)
            {
                searchHead = _headModelMax;
            }

            // If we've wrapped back to the starting point, give up
            if (searchHead == currentHead && attemptsRemaining < _headModelMax)
            {
                return currentHead;
            }

            // Check if this head is valid
            if (IsValidHeadModel(creature, searchHead))
            {
                return searchHead;
            }

            attemptsRemaining--;
        }

        return currentHead;
    }

    private bool IsValidHeadModel(NwCreature creature, int headNumber)
    {
        if (headNumber < 1 || headNumber > _headModelMax) return false;

        // Build proper head resref with phenotype
        string genderLetter = creature.Gender == Gender.Female ? "f" : "m";

        // Get race letter from appearance
        string raceStr = NWScript.Get2DAString("appearance", "RACE", creature.Appearance.RowIndex);
        string raceLetter = !string.IsNullOrEmpty(raceStr) && raceStr.Length > 0 ? raceStr.Substring(0, 1).ToLower() : "h";

        // Get phenotype (0 or 2)
        int phenotype = (int)creature.Phenotype;
        if (phenotype != 0 && phenotype != 2) phenotype = 0;

        // Build resref: prefix + phenotype + "_head" + number (e.g., "pmh0_head001")
        string prefix = $"p{genderLetter}{raceLetter}";
        string modelResRef = $"{prefix}{phenotype}_head{headNumber:D3}";

        // Check if model exists in ResMan
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);
        return !string.IsNullOrEmpty(alias);
    }

    public void SetAppearance(int newAppearance)
    {
        if (newAppearance < 0 || newAppearance > _appearanceTypeMax)
        {
            player.SendServerMessage($"Appearance must be between 0 and {_appearanceTypeMax}.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Validate using helper method
        if (!IsValidAppearance(newAppearance))
        {
            player.SendServerMessage($"Appearance {newAppearance} is not valid or does not have an allowed racial type.", ColorConstants.Orange);
            return;
        }

        AppearanceType = newAppearance;
        NWScript.SetCreatureAppearanceType(creature, newAppearance);

        // Also update the head model to match the new appearance
        HeadModel = creature.GetCreatureBodyPart(CreaturePart.Head);

        // Get the racial type for feedback
        string racialTypeStr = NWScript.Get2DAString("appearance", "RACIALTYPE", newAppearance);
        int.TryParse(racialTypeStr, out int racialType);

        player.SendServerMessage($"Appearance changed to {newAppearance} (RACIALTYPE: {racialType}).", ColorConstants.Green);
    }

    public void ModifyAppearance(int delta)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Search for the next valid appearance
        int newAppearance = FindNextValidAppearance(AppearanceType, delta);

        if (newAppearance == AppearanceType)
        {
            player.SendServerMessage("No other valid appearances found.", ColorConstants.Orange);
            return;
        }

        AppearanceType = newAppearance;
        NWScript.SetCreatureAppearanceType(creature, newAppearance);

        // Also update the head model to match the new appearance
        HeadModel = creature.GetCreatureBodyPart(CreaturePart.Head);

        // Get the racial type for feedback
        string racialTypeStr = NWScript.Get2DAString("appearance", "RACIALTYPE", newAppearance);
        int.TryParse(racialTypeStr, out int racialType);

        player.SendServerMessage($"Appearance changed to {newAppearance} (RACIALTYPE: {racialType}).", ColorConstants.Green);
    }

    private int FindNextValidAppearance(int currentAppearance, int delta)
    {
        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchAppearance = currentAppearance;
        int attemptsRemaining = _appearanceTypeMax + 1;

        while (attemptsRemaining > 0)
        {
            // Move by the delta amount (10 or 1)
            if (step >= 10)
            {
                searchAppearance += delta;
                step = 1; // After the first jump, search one by one
            }
            else
            {
                searchAppearance += direction;
            }

            // Wrap around
            if (searchAppearance > _appearanceTypeMax)
            {
                searchAppearance = 0;
            }
            else if (searchAppearance < 0)
            {
                searchAppearance = _appearanceTypeMax;
            }

            // If we've wrapped back to the starting point, give up
            if (searchAppearance == currentAppearance && attemptsRemaining < _appearanceTypeMax)
            {
                return currentAppearance;
            }

            // Check if this appearance is valid (has allowed racial type)
            if (IsValidAppearance(searchAppearance))
            {
                return searchAppearance;
            }

            attemptsRemaining--;
        }

        return currentAppearance;
    }

    private bool IsValidAppearance(int appearanceNumber)
    {
        if (appearanceNumber < 0 || appearanceNumber > _appearanceTypeMax) return false;

        // Check if appearance exists in appearance.2da by reading RACIALTYPE column
        string racialTypeStr = NWScript.Get2DAString("appearance", "RACIALTYPE", appearanceNumber);

        if (string.IsNullOrEmpty(racialTypeStr)) return false;

        // Parse the racial type integer
        if (!int.TryParse(racialTypeStr, out int racialType)) return false;

        // Check if this racial type is allowed
        return AllowedRacialTypes.Contains(racialType);
    }

    public void SetScale(float newScale)
    {
        newScale = Math.Clamp(newScale, MinScale, MaxScale);

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        Scale = newScale;
        creature.VisualTransform.Scale = newScale;
        player.SendServerMessage($"Visual scale changed to {newScale * 100:F0}%.", ColorConstants.Green);
    }

    public void ModifyScale(float delta)
    {
        SetScale(Scale + delta);
    }

    public void SetTemporaryName(string tempName)
    {
        if (string.IsNullOrWhiteSpace(tempName))
        {
            player.SendServerMessage("Please enter a temporary name.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null)
        {
            player.SendServerMessage("Error: Could not get controlled creature.", ColorConstants.Red);
            return;
        }

        // Use PlayerNameOverrideService to set the player name override
        // This changes the player name shown in chat, etc., but NOT the character name
        var nameOverride = new PlayerNameOverride(tempName, player.PlayerName);

        player.SendServerMessage($"Attempting to set temporary name to: {tempName}", ColorConstants.Cyan);
        RenamePlugin.SetPCNameOverride(player.LoginCreature, tempName);
        //playerNameOverrideService.SetPlayerNameOverride(player, nameOverride);
        player.SendServerMessage($"Temporary name set to: {tempName}", ColorConstants.Green);
        player.SendServerMessage($"Current player name: {player.PlayerName}", ColorConstants.Yellow);
    }

    public void RestoreOriginalName()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null)
        {
            player.SendServerMessage("Error: Could not get controlled creature.", ColorConstants.Red);
            return;
        }

        player.SendServerMessage($"Attempting to restore original name...", ColorConstants.Cyan);
        // Use PlayerNameOverrideService to clear the player name override
        RenamePlugin.ClearPCNameOverride(player.LoginCreature);
        //playerNameOverrideService.ClearPlayerNameOverride(player);
        player.SendServerMessage("Original name restored.", ColorConstants.Green);
        player.SendServerMessage($"Current player name: {player.PlayerName}", ColorConstants.Yellow);
    }

    public void SetSoundset(int soundsetId)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Validate soundset exists
        string soundsetResRef = NWScript.Get2DAString("soundset", "RESREF", soundsetId);
        if (string.IsNullOrEmpty(soundsetResRef))
        {
            player.SendServerMessage($"Soundset {soundsetId} does not exist.", ColorConstants.Orange);
            return;
        }

        CurrentSoundset = soundsetId;
        NewSoundset = soundsetId;
        CurrentSoundsetResRef = $"{soundsetId}";
        NewSoundsetResRef = $"{soundsetId}";
        creature.SoundSet = (ushort)soundsetId;
        player.SendServerMessage($"Soundset changed to {soundsetId}.", ColorConstants.Green);
    }

    public void SetPortrait(string portraitResRef)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        if (string.IsNullOrWhiteSpace(portraitResRef))
        {
            player.SendServerMessage("Please enter a portrait resref.", ColorConstants.Orange);
            return;
        }

        creature.PortraitResRef = portraitResRef;
        CurrentPortrait = portraitResRef;
        NewPortrait = portraitResRef;
        player.SendServerMessage($"Portrait changed to {portraitResRef}.", ColorConstants.Green);
    }

    public void SetColor(int colorIndex)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        string channelName = CurrentColorChannel switch
        {
            0 => "Skin",
            1 => "Hair",
            2 => "Tattoo 1",
            3 => "Tattoo 2",
            _ => "Unknown"
        };

        switch (CurrentColorChannel)
        {
            case 0: // Skin
                SkinColor = colorIndex;
                creature.SetColor(ColorChannel.Skin, colorIndex);
                break;
            case 1: // Hair
                HairColor = colorIndex;
                creature.SetColor(ColorChannel.Hair, colorIndex);
                break;
            case 2: // Tattoo 1
                TattooColor1 = colorIndex;
                creature.SetColor(ColorChannel.Tattoo1, colorIndex);
                break;
            case 3: // Tattoo 2
                TattooColor2 = colorIndex;
                creature.SetColor(ColorChannel.Tattoo2, colorIndex);
                break;
        }

        player.SendServerMessage($"{channelName} color set to {colorIndex}.", ColorConstants.Green);
    }

    public void SetColorChannel(int channel)
    {
        CurrentColorChannel = channel;
    }

    public int GetColorChannel() => CurrentColorChannel;

    public void SaveChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Remove backup since changes are saved
        NWScript.DeleteLocalString(creature, BackupDataKey);
    }

    public void RevertChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        string backupJson = NWScript.GetLocalString(creature, BackupDataKey);
        if (string.IsNullOrEmpty(backupJson)) return;

        BackupData? backup = JsonConvert.DeserializeObject<BackupData>(backupJson);
        if (backup == null) return;

        // Restore all values to model
        HeadModel = backup.HeadModel;
        AppearanceType = backup.AppearanceType;
        Scale = backup.Scale;
        CurrentSoundset = backup.Soundset;
        NewSoundset = backup.Soundset;
        CurrentSoundsetResRef = $"{backup.Soundset}";
        NewSoundsetResRef = $"{backup.Soundset}";
        CurrentPortrait = backup.PortraitResRef;
        NewPortrait = backup.PortraitResRef;
        SkinColor = backup.SkinColor;
        HairColor = backup.HairColor;
        TattooColor1 = backup.TattooColor1;
        TattooColor2 = backup.TattooColor2;

        // Apply to creature
        creature.SetCreatureBodyPart(CreaturePart.Head, backup.HeadModel);
        NWScript.SetCreatureAppearanceType(creature, backup.AppearanceType);
        creature.VisualTransform.Scale = backup.Scale;
        creature.SoundSet = (ushort)backup.Soundset;
        creature.PortraitResRef = backup.PortraitResRef;
        creature.SetColor(ColorChannel.Skin, backup.SkinColor);
        creature.SetColor(ColorChannel.Hair, backup.HairColor);
        creature.SetColor(ColorChannel.Tattoo1, backup.TattooColor1);
        creature.SetColor(ColorChannel.Tattoo2, backup.TattooColor2);

        // Restore original player name by clearing override
        playerNameOverrideService.ClearPlayerNameOverride(player);

        // Don't delete backup - keep it for next revert
    }

    private class BackupData
    {
        public int HeadModel { get; set; }
        public int AppearanceType { get; set; }
        public float Scale { get; set; }
        public int Soundset { get; set; }
        public string PortraitResRef { get; set; } = "";
        public int SkinColor { get; set; }
        public int HairColor { get; set; }
        public int TattooColor1 { get; set; }
        public int TattooColor2 { get; set; }
    }
}

