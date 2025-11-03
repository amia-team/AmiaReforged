using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class CharacterCustomizationModel
{
    private readonly NwPlayer _player;
    public CustomizationMode CurrentMode { get; private set; } = CustomizationMode.None;
    public int CurrentArmorPart { get; private set; }

    public int CurrentColorChannel { get; private set; } = 2;
    private NwItem? _currentArmor;
    private NwItem? _initialArmorBackup;
    private static readonly Dictionary<int, HashSet<int>> TorsoModelsByAc = new()
    {
        [0] = new HashSet<int> { 1, 3, 5, 6, 7, 8, 9, 12, 19, 39, 50, 66, 67, 73, 74, 150, 158, 199, 200, 210, 228, 239, 240, 251 }, // Cloth
        [1] = new HashSet<int> { 20, 28, 40 }, // Padded
        [2] = new HashSet<int> { 10, 13, 16, 27, 41, 42, 49, 58, 75, 76, 77, 86, 91, 92 }, // Hide
        [3] = new HashSet<int> { 22, 29, 43, 44 }, // Studded
        [4] = new HashSet<int> { 4, 15, 18, 34, 35, 36, 38, 54, 55, 56, 59, 63, 64, 68, 69, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105 }, // Scale
        [5] = new HashSet<int> { 24, 25, 26, 31, 32, 204 }, // Chain
        [6] = new HashSet<int> { 11, 17, 30, 45, 48 }, // Banded
        [7] = new HashSet<int> { 33, 46, 47, 51, 52 }, // Halfplate
        [8] = new HashSet<int> { 14, 21, 23, 37, 53, 57, 60, 61, 62, 65, 70, 71, 72, 90, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 186, 190, 209, 220, 221, 222, 223, 252 } // Fullplate
    };

    private static readonly Dictionary<CreaturePart, HashSet<int>> ValidModelsByPart = new()
    {
        [CreaturePart.Neck] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 23, 25, 26, 28, 63, 70, 71, 127, 129, 131, 132, 133, 134, 186 },
        [CreaturePart.Belt] = new HashSet<int> { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 31, 32, 37, 39, 40, 41, 42, 43, 44, 63, 71, 96, 108, 186, 189, 190, 191 },
        [CreaturePart.Pelvis] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 63, 157, 186, 190 },
        [CreaturePart.RightShoulder] = new HashSet<int> { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 40, 41, 42, 43, 44, 96, 100, 103, 106, 186, 190 },
        [CreaturePart.LeftShoulder] = new HashSet<int> { 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 40, 41, 42, 43, 44, 96, 100, 103, 106, 186, 190 },
        [CreaturePart.RightBicep] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 63, 186 },
        [CreaturePart.LeftBicep] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 63, 186 },
        [CreaturePart.RightForearm] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 63, 186 },
        [CreaturePart.LeftForearm] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 63, 186 },
        [CreaturePart.RightHand] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 63, 186 },
        [CreaturePart.LeftHand] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 63, 186 },
        [CreaturePart.RightThigh] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 39, 63, 186, 190 },
        [CreaturePart.LeftThigh] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 39, 63, 186, 190 },
        [CreaturePart.RightShin] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 39, 40, 63, 186, 190, 195, 196, 197 },
        [CreaturePart.LeftShin] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 39, 40, 63, 186, 190, 195, 196, 197 },
        [CreaturePart.RightFoot] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 39, 63, 160, 186, 195, 196, 197, 198, 199, 200 },
        [CreaturePart.LeftFoot] = new HashSet<int> { 1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 39, 63, 160, 186, 195, 196, 197, 198, 199, 200 },
        [CreaturePart.Robe] = new HashSet<int> { 0, 3, 4, 5, 6, 10, 11, 12, 13, 15, 16, 20, 21, 27, 30, 31, 32, 33, 38, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 100, 110, 111, 112, 114, 115, 118, 122, 123, 124, 125, 126, 127, 128, 135, 143, 145, 147, 152, 154, 159, 164, 166, 167, 168, 182, 183, 186, 187, 188, 189, 190, 191, 192, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254 }
    };

    public CharacterCustomizationModel(NwPlayer player)
    {
        _player = player;
    }

    private void LoadCurrentArmor()
    {
        NwCreature? creature = _player.ControlledCreature;
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
                NwCreature? creature = _player.ControlledCreature;
                if (creature != null)
                {
                    _initialArmorBackup = _currentArmor.Clone(creature, null, false);
                }

                string armorName = _currentArmor.Name;
                _player.SendServerMessage($"Modifying {armorName}. Select part, model, and color.", ColorConstants.Cyan);
            }
            else
            {
                _player.SendServerMessage("You must be wearing armor to customize it.", ColorConstants.Orange);
            }
        }
    }

    public void SetArmorPart(int partIndex)
    {
        if (partIndex >= 0 && partIndex <= 19) // 0-18 for individual parts, 19 for "All Parts"
        {
            CurrentArmorPart = partIndex;
        }
    }

    public void SetColorChannel(int channel)
    {
        if (channel >= 0 && channel <= 5)
        {
            CurrentColorChannel = channel;
        }
    }

    public void AdjustArmorPartModel(int delta)
    {
        if (CurrentMode != CustomizationMode.Armor) return;

        if (CurrentArmorPart == 19)
        {
            _player.SendServerMessage("Cannot adjust models in 'All Parts' mode. Select a specific part first.", ColorConstants.Orange);
            return;
        }

        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            _player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);

        int currentModel = _currentArmor.Appearance.GetArmorModel(creaturePart);
        int newModel;

        if (creaturePart == CreaturePart.Torso)
        {
            int? currentAc = GetArmorClassFromTorsoModel(currentModel);

            if (currentAc.HasValue && TorsoModelsByAc.TryGetValue(currentAc.Value, out HashSet<int>? validModels))
            {
                List<int> sortedModels = validModels.OrderBy(m => m).ToList();
                int currentIndex = sortedModels.IndexOf(currentModel);

                if (currentIndex >= 0)
                {
                    if (Math.Abs(delta) == 1)
                    {
                        int newIndex = currentIndex + delta;
                        if (newIndex < 0)
                            newIndex = sortedModels.Count - 1;
                        else if (newIndex >= sortedModels.Count)
                            newIndex = 0;

                        newModel = sortedModels[newIndex];
                    }
                    else
                    {
                        int targetModel = currentModel + delta;

                        if (delta > 0)
                        {
                            newModel = sortedModels.FirstOrDefault(m => m >= targetModel);
                            if (newModel == 0)
                                newModel = sortedModels.First();
                        }
                        else
                        {
                            newModel = sortedModels.LastOrDefault(m => m <= targetModel);
                            if (newModel == 0)
                                newModel = sortedModels.Last();
                        }
                    }
                }
                else
                {
                    newModel = sortedModels.FirstOrDefault();
                }
            }
            else
            {
                _player.SendServerMessage("Could not determine armor class. No change made.", ColorConstants.Orange);
                return;
            }
        }
        else
        {
            if (ValidModelsByPart.TryGetValue(creaturePart, out HashSet<int>? validModels))
            {
                List<int> sortedModels = validModels.OrderBy(m => m).ToList();
                int currentIndex = sortedModels.IndexOf(currentModel);

                if (currentIndex >= 0)
                {
                    if (Math.Abs(delta) == 1)
                    {
                        int newIndex = currentIndex + delta;
                        if (newIndex < 0)
                            newIndex = sortedModels.Count - 1;
                        else if (newIndex >= sortedModels.Count)
                            newIndex = 0;

                        newModel = sortedModels[newIndex];
                    }
                    else
                    {
                        int targetModel = currentModel + delta;

                        if (delta > 0)
                        {
                            newModel = sortedModels.FirstOrDefault(m => m >= targetModel);
                            if (newModel == 0)
                                newModel = sortedModels.First();
                        }
                        else
                        {
                            newModel = sortedModels.LastOrDefault(m => m <= targetModel);
                            if (newModel == 0)
                                newModel = sortedModels.Last();
                        }
                    }
                }
                else
                {
                    newModel = sortedModels.FirstOrDefault();
                }
            }
            else
            {
                _player.SendServerMessage("No valid models for this part.", ColorConstants.Orange);
                return;
            }
        }

        NwItem oldArmor = _currentArmor;

        oldArmor.Appearance.SetArmorModel(creaturePart, (byte)newModel);
        creature.RunUnequip(oldArmor);
        NwItem newArmor = oldArmor.Clone(creature);

        if (!newArmor.IsValid)
        {
            _player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        creature.RunEquip(newArmor, InventorySlot.Chest);
        _currentArmor = newArmor;
        oldArmor.Destroy();
        _player.SendServerMessage($"Part model updated to {newModel}.", ColorConstants.Green);
    }

    public void SetArmorPartColor(int colorIndex)
    {
        if (CurrentMode != CustomizationMode.Armor) return;
        if (colorIndex < 0 || colorIndex > 175) return;
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            _player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentColorChannel);

        string[] channelNames = new[] { "Leather 1", "Leather 2", "Cloth 1", "Cloth 2", "Metal 1", "Metal 2" };
        string channelName = CurrentColorChannel < channelNames.Length ? channelNames[CurrentColorChannel] : "Unknown";

        NwItem oldArmor = _currentArmor;

        if (CurrentArmorPart == 19)
        {
            CreaturePart[] allParts = new[]
            {
                CreaturePart.RightFoot, CreaturePart.LeftFoot, CreaturePart.RightShin, CreaturePart.LeftShin,
                CreaturePart.RightThigh, CreaturePart.LeftThigh, CreaturePart.Pelvis, CreaturePart.Torso,
                CreaturePart.Belt, CreaturePart.Neck, CreaturePart.RightForearm, CreaturePart.LeftForearm,
                CreaturePart.RightBicep, CreaturePart.LeftBicep, CreaturePart.RightShoulder, CreaturePart.LeftShoulder,
                CreaturePart.RightHand, CreaturePart.LeftHand, CreaturePart.Robe
            };

            foreach (CreaturePart part in allParts)
            {
                oldArmor.Appearance.SetArmorPieceColor(part, colorChannel, (byte)colorIndex);
            }

            _player.SendServerMessage($"Applying {channelName} color to all armor parts...", ColorConstants.Cyan);
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
            _player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        creature.RunEquip(newArmor, InventorySlot.Chest);
        _currentArmor = newArmor;
        oldArmor.Destroy();

        if (CurrentArmorPart == 19)
        {
            _player.SendServerMessage($"All armor parts {channelName} color updated to {colorIndex}.", ColorConstants.Green);
        }
        else
        {
            string[] partNames = new[] { "Right Foot", "Left Foot", "Right Shin", "Left Shin",
                "Right Thigh", "Left Thigh", "Pelvis", "Torso", "Belt", "Neck",
                "Right Forearm", "Left Forearm", "Right Bicep", "Left Bicep",
                "Right Shoulder", "Left Shoulder", "Right Hand", "Left Hand", "Robe" };
            string partName = CurrentArmorPart < partNames.Length ? partNames[CurrentArmorPart] : "Unknown";

            _player.SendServerMessage($"{partName} {channelName} color updated to {colorIndex}.", ColorConstants.Green);
        }
    }

    public void SetHairColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _player.SendServerMessage($"Hair color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
    }

    public void SetTattoo1Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _player.SendServerMessage($"Tattoo1 color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
    }

    public void SetTattoo2Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _player.SendServerMessage($"Tattoo2 color {colorIndex} - feature coming soon!", ColorConstants.Yellow);
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

    private int? GetArmorClassFromTorsoModel(int modelIndex)
    {
        foreach (KeyValuePair<int, HashSet<int>> kvp in TorsoModelsByAc)
        {
            if (kvp.Value.Contains(modelIndex))
            {
                return kvp.Key;
            }
        }
        return null;
    }

    public void ApplyChanges()
    {
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            _player.SendServerMessage("No armor to save.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        if (_initialArmorBackup != null && _initialArmorBackup.IsValid)
        {
            _initialArmorBackup.Destroy();
        }

        _initialArmorBackup = _currentArmor.Clone(creature, null, false);
        _player.SendServerMessage("Armor customization saved! You can continue editing or click Revert to return to this save point. Close the window normally when you're done!", ColorConstants.Green);
    }

    public void RevertChanges()
    {
        if (_initialArmorBackup == null || !_initialArmorBackup.IsValid)
        {
            _player.SendServerMessage("No changes to revert.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        if (_currentArmor != null && _currentArmor.IsValid)
        {
            creature.RunUnequip(_currentArmor);
            _currentArmor.Destroy();
        }

        NwItem restoredArmor = _initialArmorBackup.Clone(creature);

        if (restoredArmor.IsValid)
        {
            creature.RunEquip(restoredArmor, InventorySlot.Chest);
            _currentArmor = restoredArmor;

            _player.SendServerMessage("Armor customization reverted to last save point.", ColorConstants.Cyan);
        }
        else
        {
            _player.SendServerMessage("Failed to revert armor changes.", ColorConstants.Red);
        }

        _initialArmorBackup.Destroy();
        _initialArmorBackup = null;
    }

    public void ConfirmAndClose()
    {
        if (_initialArmorBackup != null && _initialArmorBackup.IsValid)
        {
            _initialArmorBackup.Destroy();
            _initialArmorBackup = null;
        }

        _player.SendServerMessage("Armor customization confirmed!", ColorConstants.Green);
    }
}

public enum CustomizationMode
{
    None,
    Armor,
    Equipment,
    Appearance
}

