using Anvil.API;
using Anvil.API.Events;
using Newtonsoft.Json;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class CharacterCustomizationModel(NwPlayer player)
{
    public CustomizationMode CurrentMode { get; private set; } = CustomizationMode.None;
    public int CurrentArmorPart { get; private set; }

    private int CurrentColorChannel { get; set; } = 2;
    private NwItem? _currentArmor;
    private const string BackupDataKey = "ARMOR_CUSTOMIZATION_BACKUP";

    private static readonly Dictionary<int, HashSet<int>> TorsoModelsByAc = new()
    {
        [0] =
        [
            1, 3, 5, 6, 7, 8, 9, 12, 19, 39, 50, 66, 67, 73, 74, 150, 158, 199, 200, 210, 228, 239, 240, 251
        ], // Cloth
        [1] = [20, 28, 40], // Padded
        [2] = [10, 13, 16, 27, 41, 42, 49, 58, 75, 76, 77, 86, 91, 92], // Hide
        [3] = [22, 29, 43, 44], // Studded
        [4] =
        [
            4, 15, 18, 34, 35, 36, 38, 54, 55, 56, 59, 63, 64, 68, 69, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103,
            104, 105
        ], // Scale
        [5] = [24, 25, 26, 31, 32, 204], // Chain
        [6] = [11, 17, 30, 45, 48], // Banded
        [7] = [33, 46, 47, 51, 52], // Half-plate
        [8] =
        [
            14, 21, 23, 37, 53, 57, 60, 61, 62, 65, 70, 71, 72, 90, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115,
            116, 117, 186, 190, 209, 220, 221, 222, 223, 252
        ] // Full plate
    };

    private void LoadCurrentArmor()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        _currentArmor = creature.GetItemInSlot(InventorySlot.Chest);
    }

    public void SetMode(CustomizationMode mode)
    {
        CurrentMode = mode;
        CurrentArmorPart = 0;

        if (mode == CustomizationMode.Armor)
        {
            LoadCurrentArmor();
            if (_currentArmor != null && _currentArmor.IsValid)
            {
                NwCreature? creature = player.ControlledCreature;
                if (creature != null)
                {
                    SaveBackupToPcKey();
                }

                string armorName = _currentArmor.Name;
                player.SendServerMessage($"Modifying {armorName}. Select part, model, and color.",
                    ColorConstants.Cyan);
            }
            else
            {
                player.SendServerMessage("You must be wearing armor to customize it.", ColorConstants.Orange);
            }
        }
    }

    public void SetArmorPart(int partIndex)
    {
        if (partIndex is >= 0 and <= 19) // 0-18 for individual parts, 19 for "All Parts"
        {
            CurrentArmorPart = partIndex;
        }
    }

    public void SetColorChannel(int channel)
    {
        if (channel is >= 0 and <= 5)
        {
            CurrentColorChannel = channel;
        }
    }

    public void AdjustArmorPartModel(int delta)
    {
        if (CurrentMode != CustomizationMode.Armor) return;

        if (CurrentArmorPart == 19)
        {
            player.SendServerMessage("Cannot adjust models in 'All Parts' mode. Select a specific part first.",
                ColorConstants.Orange);
            return;
        }

        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);
        int currentModel = _currentArmor.Appearance.GetArmorModel(creaturePart);

        int newModel = GetNextValidArmorModel(creaturePart, currentModel, delta);

        if (newModel == currentModel)
        {
            return;
        }

        NwItem oldArmor = _currentArmor;

        oldArmor.Appearance.SetArmorModel(creaturePart, (byte)newModel);
        creature.RunUnequip(oldArmor);
        NwItem newArmor = oldArmor.Clone(creature);

        if (!newArmor.IsValid)
        {
            player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        creature.RunEquip(newArmor, InventorySlot.Chest);
        _currentArmor = newArmor;
        oldArmor.Destroy();
        player.SendServerMessage($"Part model updated to {newModel}.", ColorConstants.Green);
    }

    private int GetNextValidArmorModel(CreaturePart creaturePart, int currentModel, int delta)
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return currentModel;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return currentModel;

        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchModel = currentModel;
        int maxModel = 255;
        int attemptsRemaining = maxModel + 1;

        while (attemptsRemaining > 0)
        {
            if (step == 1)
            {
                searchModel += direction;
            }
            else
            {
                searchModel += delta;
                step = 1;
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
                player.SendServerMessage("No other valid models found.", ColorConstants.Orange);
                return currentModel;
            }

            if (IsValidArmorModel(creaturePart, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid armor model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidArmorModel(CreaturePart creaturePart, int modelNumber)
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return false;

        if (modelNumber < 0) return false;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return false;

        if (creaturePart == CreaturePart.Torso)
        {
            int? currentAc = GetArmorAcFromModel(_currentArmor.Appearance.GetArmorModel(CreaturePart.Torso));

            if (currentAc.HasValue && TorsoModelsByAc.TryGetValue(currentAc.Value, out HashSet<int>? validModels))
            {
                if (!validModels.Contains(modelNumber))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

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

        int phenotype = (int)creature.Phenotype;

        string partName = GetArmorPartName(creaturePart);
        string sideLetter = GetArmorPartSide(creaturePart);

        if (string.IsNullOrEmpty(partName)) return false;

        string modelResRef = $"p{genderLetter}{raceLetter}{phenotype}_{partName}{sideLetter}{modelNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);
        return !string.IsNullOrEmpty(alias);
    }

    private int? GetArmorAcFromModel(int modelNumber)
    {
        foreach (KeyValuePair<int, HashSet<int>> kvp in TorsoModelsByAc)
        {
            if (kvp.Value.Contains(modelNumber))
            {
                return kvp.Key;
            }
        }
        return null;
    }

    private string GetArmorPartName(CreaturePart part)
    {
        return part switch
        {
            CreaturePart.RightFoot => "foot",
            CreaturePart.LeftFoot => "foot",
            CreaturePart.RightShin => "shin",
            CreaturePart.LeftShin => "shin",
            CreaturePart.RightThigh => "leg",
            CreaturePart.LeftThigh => "leg",
            CreaturePart.Pelvis => "pelvis",
            CreaturePart.Torso => "chest",
            CreaturePart.Belt => "belt",
            CreaturePart.Neck => "neck",
            CreaturePart.RightForearm => "fore",
            CreaturePart.LeftForearm => "fore",
            CreaturePart.RightBicep => "bicep",
            CreaturePart.LeftBicep => "bicep",
            CreaturePart.RightShoulder => "shol",
            CreaturePart.LeftShoulder => "shol",
            CreaturePart.RightHand => "hand",
            CreaturePart.LeftHand => "hand",
            CreaturePart.Robe => "robe",
            _ => ""
        };
    }

    private string GetArmorPartSide(CreaturePart part)
    {
        return part switch
        {
            CreaturePart.LeftFoot => "l",
            CreaturePart.LeftShin => "l",
            CreaturePart.LeftThigh => "l",
            CreaturePart.LeftForearm => "l",
            CreaturePart.LeftBicep => "l",
            CreaturePart.LeftHand => "l",

            CreaturePart.RightFoot => "r",
            CreaturePart.RightShin => "r",
            CreaturePart.RightThigh => "r",
            CreaturePart.RightForearm => "r",
            CreaturePart.RightBicep => "r",
            CreaturePart.RightHand => "r",

            _ => ""
        };
    }


    public void SetArmorPartColor(int colorIndex)
    {
        if (CurrentMode != CustomizationMode.Armor) return;
        if (colorIndex < 0 || colorIndex > 175) return;
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentColorChannel);

        string[] channelNames = ["Leather 1", "Leather 2", "Cloth 1", "Cloth 2", "Metal 1", "Metal 2"];
        string channelName = CurrentColorChannel < channelNames.Length ? channelNames[CurrentColorChannel] : "Unknown";

        NwItem oldArmor = _currentArmor;

        if (CurrentArmorPart == 19)
        {
            CreaturePart[] allParts =
            [
                CreaturePart.RightFoot, CreaturePart.LeftFoot, CreaturePart.RightShin, CreaturePart.LeftShin,
                CreaturePart.RightThigh, CreaturePart.LeftThigh, CreaturePart.Pelvis, CreaturePart.Torso,
                CreaturePart.Belt, CreaturePart.Neck, CreaturePart.RightForearm, CreaturePart.LeftForearm,
                CreaturePart.RightBicep, CreaturePart.LeftBicep, CreaturePart.RightShoulder, CreaturePart.LeftShoulder,
                CreaturePart.RightHand, CreaturePart.LeftHand, CreaturePart.Robe
            ];

            foreach (CreaturePart part in allParts)
            {
                oldArmor.Appearance.SetArmorPieceColor(part, colorChannel, (byte)colorIndex);
            }

            player.SendServerMessage($"Applying {channelName} color to all armor parts...", ColorConstants.Cyan);
        }
        else
        {
            CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);
            oldArmor.Appearance.SetArmorPieceColor(creaturePart, colorChannel, (byte)colorIndex);
        }

        creature.RunUnequip(oldArmor);
        NwItem newArmor = oldArmor.Clone(creature);

        if (!newArmor.IsValid)
        {
            player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        creature.RunEquip(newArmor, InventorySlot.Chest);
        _currentArmor = newArmor;
        oldArmor.Destroy();

        if (CurrentArmorPart == 19)
        {
            player.SendServerMessage($"All armor parts {channelName} color updated to {colorIndex}.",
                ColorConstants.Green);
        }
        else
        {
            string[] partNames =
            [
                "Right Foot", "Left Foot", "Right Shin", "Left Shin",
                "Right Thigh", "Left Thigh", "Pelvis", "Torso", "Belt", "Neck",
                "Right Forearm", "Left Forearm", "Right Bicep", "Left Bicep",
                "Right Shoulder", "Left Shoulder", "Right Hand", "Left Hand", "Robe"
            ];
            string partName = CurrentArmorPart < partNames.Length ? partNames[CurrentArmorPart] : "Unknown";

            player.SendServerMessage($"{partName} {channelName} color updated to {colorIndex}.", ColorConstants.Green);
        }
    }

    public void SetHairColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        player.SendServerMessage($"Hair color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
    }

    public void SetTattoo1Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        player.SendServerMessage($"Tattoo1 color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
    }

    public void SetTattoo2Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        player.SendServerMessage($"Tattoo2 color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
    }

    public int GetCurrentArmorPartModel()
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return 0;
        if (CurrentArmorPart == 19) return 0;
        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);
        return _currentArmor.Appearance.GetArmorModel(creaturePart);
    }

    public int GetCurrentArmorPartColor()
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return 0;
        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentColorChannel);
        if (CurrentArmorPart == 19)
        {
            return _currentArmor.Appearance.GetArmorPieceColor(CreaturePart.Torso, colorChannel);
        }

        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);
        return _currentArmor.Appearance.GetArmorPieceColor(creaturePart, colorChannel);
    }

    private CreaturePart GetCreaturePart(int partIndex)
    {
        return partIndex switch
        {
            0 => CreaturePart.RightFoot,
            1 => CreaturePart.LeftFoot,
            2 => CreaturePart.RightShin,
            3 => CreaturePart.LeftShin,
            4 => CreaturePart.RightThigh,
            5 => CreaturePart.LeftThigh,
            6 => CreaturePart.Pelvis,
            7 => CreaturePart.Torso,
            8 => CreaturePart.Belt,
            9 => CreaturePart.Neck,
            10 => CreaturePart.RightForearm,
            11 => CreaturePart.LeftForearm,
            12 => CreaturePart.RightBicep,
            13 => CreaturePart.LeftBicep,
            14 => CreaturePart.RightShoulder,
            15 => CreaturePart.LeftShoulder,
            16 => CreaturePart.RightHand,
            17 => CreaturePart.LeftHand,
            18 => CreaturePart.Robe,
            _ => CreaturePart.Torso
        };
    }

    private ItemAppearanceArmorColor GetArmorColorChannel(int channelIndex)
    {
        return channelIndex switch
        {
            0 => ItemAppearanceArmorColor.Leather1,
            1 => ItemAppearanceArmorColor.Leather2,
            2 => ItemAppearanceArmorColor.Cloth1,
            3 => ItemAppearanceArmorColor.Cloth2,
            4 => ItemAppearanceArmorColor.Metal1,
            5 => ItemAppearanceArmorColor.Metal2,
            _ => ItemAppearanceArmorColor.Leather1
        };
    }


    public void ApplyChanges()
    {
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            player.SendServerMessage("No armor to save.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        SaveBackupToPcKey();
        player.SendServerMessage(
            "Armor customization saved! You can continue editing or click Revert to return to this save point.",
            ColorConstants.Green);
    }

    public void RevertChanges()
    {
        ArmorBackupData? backupData = LoadBackupFromPcKey();
        if (backupData == null)
        {
            player.SendServerMessage("No changes to revert.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        if (_currentArmor != null && _currentArmor.IsValid)
        {
            creature.RunUnequip(_currentArmor);
            NwItem newArmor = _currentArmor.Clone(creature);

            if (newArmor.IsValid)
            {
                backupData.ApplyToItem(newArmor);

                creature.RunEquip(newArmor, InventorySlot.Chest);
                _currentArmor.Destroy();
                _currentArmor = newArmor;
                player.SendServerMessage("Armor customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                creature.RunEquip(_currentArmor, InventorySlot.Chest);
                player.SendServerMessage("Failed to revert armor changes.", ColorConstants.Red);
            }
        }
    }

    public void ConfirmAndClose()
    {
        ClearBackupFromPcKey();
        player.SendServerMessage("Armor customization confirmed!", ColorConstants.Green);
    }

    public void CopyToOtherSide()
    {
        if (CurrentMode != CustomizationMode.Armor)
        {
            player.SendServerMessage("Not in armor customization mode.", ColorConstants.Orange);
            return;
        }

        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null)
        {
            player.SendServerMessage("No controlled creature found.", ColorConstants.Red);
            return;
        }

        // Map of left/right pairs
        Dictionary<int, int> sideMapping = new()
        {
            [0] = 1,   // Right Foot <-> Left Foot
            [1] = 0,   // Left Foot <-> Right Foot
            [2] = 3,   // Right Shin <-> Left Shin
            [3] = 2,   // Left Shin <-> Right Shin
            [4] = 5,   // Right Thigh <-> Left Thigh
            [5] = 4,   // Left Thigh <-> Right Thigh
            [10] = 11, // Right Forearm <-> Left Forearm
            [11] = 10, // Left Forearm <-> Right Forearm
            [12] = 13, // Right Bicep <-> Left Bicep
            [13] = 12, // Left Bicep <-> Right Bicep
            [14] = 15, // Right Shoulder <-> Left Shoulder
            [15] = 14, // Left Shoulder <-> Right Shoulder
            [16] = 17, // Right Hand <-> Left Hand
            [17] = 16  // Left Hand <-> Right Hand
        };

        if (!sideMapping.ContainsKey(CurrentArmorPart))
        {
            player.SendServerMessage("Current part doesn't have an opposite side.", ColorConstants.Orange);
            return;
        }

        int oppositePart = sideMapping[CurrentArmorPart];
        CreaturePart sourcePart = GetCreaturePart(CurrentArmorPart);
        CreaturePart targetPart = GetCreaturePart(oppositePart);

        string[] partNames =
        [
            "Right Foot", "Left Foot", "Right Shin", "Left Shin",
            "Right Thigh", "Left Thigh", "Pelvis", "Torso", "Belt", "Neck",
            "Right Forearm", "Left Forearm", "Right Bicep", "Left Bicep",
            "Right Shoulder", "Left Shoulder", "Right Hand", "Left Hand", "Robe"
        ];

        string sourceName = CurrentArmorPart < partNames.Length ? partNames[CurrentArmorPart] : "Unknown";
        string targetName = oppositePart < partNames.Length ? partNames[oppositePart] : "Unknown";

        player.SendServerMessage($"Copying {sourceName} to {targetName}...", ColorConstants.Cyan);

        NwItem oldArmor = _currentArmor;

        // Copy model
        int sourceModel = oldArmor.Appearance.GetArmorModel(sourcePart);
        oldArmor.Appearance.SetArmorModel(targetPart, (byte)sourceModel);
        player.SendServerMessage($"Model copied: {sourceModel}", ColorConstants.Gray);

        // Copy all color channels
        ItemAppearanceArmorColor[] colorChannels =
        [
            ItemAppearanceArmorColor.Leather1, ItemAppearanceArmorColor.Leather2,
            ItemAppearanceArmorColor.Cloth1, ItemAppearanceArmorColor.Cloth2,
            ItemAppearanceArmorColor.Metal1, ItemAppearanceArmorColor.Metal2
        ];

        foreach (ItemAppearanceArmorColor channel in colorChannels)
        {
            int sourceColor = oldArmor.Appearance.GetArmorPieceColor(sourcePart, channel);
            oldArmor.Appearance.SetArmorPieceColor(targetPart, channel, (byte)sourceColor);
        }

        player.SendServerMessage("Colors copied.", ColorConstants.Gray);

        // Refresh armor
        creature.RunUnequip(oldArmor);
        NwItem newArmor = oldArmor.Clone(creature);

        if (!newArmor.IsValid)
        {
            player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        creature.RunEquip(newArmor, InventorySlot.Chest);
        _currentArmor = newArmor;
        oldArmor.Destroy();

        player.SendServerMessage($"Successfully copied {sourceName} appearance to {targetName}.", ColorConstants.Green);
    }

    public void CopyAppearanceToItem(NwItem targetItem)
    {
        if (!targetItem.IsValid)
        {
            player.SendServerMessage("Invalid item selected.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        if (targetItem.BaseItem.ItemType != BaseItemType.Armor)
        {
            player.SendServerMessage("Selected item is not armor.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        ArmorBackupData? backupData = LoadBackupFromPcKey();
        if (backupData == null)
        {
            player.SendServerMessage("No backup appearance found. Save changes first.", ColorConstants.Orange);
            return;
        }

        // Get the AC value of the current armor to prevent AC changes
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            player.SendServerMessage("No armor equipped.", ColorConstants.Orange);
            return;
        }

        int? currentAc = GetArmorAcFromModel(_currentArmor.Appearance.GetArmorModel(CreaturePart.Torso));
        if (!currentAc.HasValue)
        {
            player.SendServerMessage("Could not determine armor AC.", ColorConstants.Orange);
            return;
        }

        // Check AC compatibility
        int? targetAc = GetArmorAcFromModel(targetItem.Appearance.GetArmorModel(CreaturePart.Torso));
        if (!targetAc.HasValue)
        {
            player.SendServerMessage("Could not determine target armor AC.", ColorConstants.Orange);
            return;
        }

        if (targetAc.Value != currentAc.Value)
        {
            player.SendServerMessage($"Cannot copy appearance - AC mismatch. Current armor is AC {currentAc.Value}, target is AC {targetAc.Value}.", ColorConstants.Orange);
            return;
        }

        // Clone the target item and apply backup appearance
        NwItem clonedTarget = targetItem.Clone(creature);

        if (clonedTarget.IsValid)
        {
            backupData.ApplyToItem(clonedTarget);
            targetItem.Destroy();
            player.SendServerMessage($"Copied appearance to {clonedTarget.Name}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to copy appearance.", ColorConstants.Red);
        }
    }

    private void SaveBackupToPcKey()
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return;

        ArmorBackupData? backupData = ArmorBackupData.FromItem(_currentArmor);
        if (backupData == null) return;

        string json = JsonConvert.SerializeObject(backupData);

        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.SetLocalString(pcKey, BackupDataKey, json);
            player.SendServerMessage($"Backup saved: {json.Length} characters.", ColorConstants.Gray);
        }
        else
        {
            player.SendServerMessage("Warning: PC Key not found for backup!", ColorConstants.Orange);
        }
    }

    private ArmorBackupData? LoadBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid)
        {
            player.SendServerMessage("PC Key not found when loading backup.", ColorConstants.Orange);
            return null;
        }

        string json = NWScript.GetLocalString(pcKey, BackupDataKey);
        if (string.IsNullOrEmpty(json))
        {
            player.SendServerMessage("No backup data found on PC Key.", ColorConstants.Orange);
            return null;
        }

        player.SendServerMessage($"Backup loaded: {json.Length} characters", ColorConstants.Gray);

        try
        {
            return JsonConvert.DeserializeObject<ArmorBackupData>(json);
        }
        catch
        {
            player.SendServerMessage("Failed to deserialize backup data.", ColorConstants.Red);
            return null;
        }
    }

    private void ClearBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.DeleteLocalString(pcKey, BackupDataKey);
            player.SendServerMessage("Backup cleared from PC Key.", ColorConstants.Gray);
        }
    }
}

public enum CustomizationMode
{
    None,
    Armor,
    Equipment,
    Appearance
}

public class ArmorBackupData
{
    public string ArmorResRef = "";
    public string ArmorName = "";
    public Dictionary<string, int> ArmorModels { get; set; } = new();
    public Dictionary<string, Dictionary<string, int>> ArmorColors { get; set; } = new();

    public static ArmorBackupData? FromItem(NwItem armor)
    {
        if (!armor.IsValid) return null;

        ArmorBackupData data = new ArmorBackupData
        {
            ArmorResRef = armor.ResRef,
            ArmorName = armor.Name,
            ArmorModels = new Dictionary<string, int>(),
            ArmorColors = new Dictionary<string, Dictionary<string, int>>()
        };

        CreaturePart[] allParts =
        [
            CreaturePart.RightFoot, CreaturePart.LeftFoot, CreaturePart.RightShin, CreaturePart.LeftShin,
            CreaturePart.RightThigh, CreaturePart.LeftThigh, CreaturePart.Pelvis, CreaturePart.Torso,
            CreaturePart.Belt, CreaturePart.Neck, CreaturePart.RightForearm, CreaturePart.LeftForearm,
            CreaturePart.RightBicep, CreaturePart.LeftBicep, CreaturePart.RightShoulder, CreaturePart.LeftShoulder,
            CreaturePart.RightHand, CreaturePart.LeftHand, CreaturePart.Robe
        ];

        foreach (CreaturePart part in allParts)
        {
            data.ArmorModels[part.ToString()] = armor.Appearance.GetArmorModel(part);
        }

        ItemAppearanceArmorColor[] colorChannels =
        [
            ItemAppearanceArmorColor.Leather1, ItemAppearanceArmorColor.Leather2,
            ItemAppearanceArmorColor.Cloth1, ItemAppearanceArmorColor.Cloth2,
            ItemAppearanceArmorColor.Metal1, ItemAppearanceArmorColor.Metal2
        ];

        foreach (CreaturePart part in allParts)
        {
            Dictionary<string, int> partColors = new Dictionary<string, int>();
            foreach (ItemAppearanceArmorColor channel in colorChannels)
            {
                partColors[channel.ToString()] = armor.Appearance.GetArmorPieceColor(part, channel);
            }

            data.ArmorColors[part.ToString()] = partColors;
        }

        return data;
    }

    public void ApplyToItem(NwItem armor)
    {
        if (!armor.IsValid) return;

        int appliedModels = 0;
        int appliedColors = 0;

        foreach (KeyValuePair<string, int> kvp in ArmorModels)
        {
            if (Enum.TryParse<CreaturePart>(kvp.Key, out CreaturePart part))
            {
                armor.Appearance.SetArmorModel(part, (byte)kvp.Value);
                appliedModels++;
            }
        }

        foreach (KeyValuePair<string, Dictionary<string, int>> partKvp in ArmorColors)
        {
            if (Enum.TryParse<CreaturePart>(partKvp.Key, out CreaturePart part))
            {
                foreach (KeyValuePair<string, int> colorKvp in partKvp.Value)
                {
                    if (Enum.TryParse<ItemAppearanceArmorColor>(colorKvp.Key, out ItemAppearanceArmorColor channel))
                    {
                        armor.Appearance.SetArmorPieceColor(part, channel, (byte)colorKvp.Value);
                        appliedColors++;
                    }
                }
            }
        }

        if (armor.RootPossessor != null)
        {
            NWScript.SendMessageToPC(armor.RootPossessor, $"DEBUG: Applied {appliedModels} models and {appliedColors} colors");
        }
    }
}
