using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

/// <summary>
/// Model for character customization - handles business logic for appearance changes
/// </summary>
public sealed class CharacterCustomizationModel
{
    private readonly NwPlayer _player;

    // Current customization mode - start with None
    public CustomizationMode CurrentMode { get; private set; } = CustomizationMode.None;

    // Current armor part being edited (for armor mode)
    public int CurrentArmorPart { get; private set; }

    // Current color channel (0=Leather1, 1=Leather2, 2=Cloth1, 3=Cloth2, 4=Metal1, 5=Metal2)
    public int CurrentColorChannel { get; private set; } = 2; // Default to Cloth 1

    // Current armor being edited
    private NwItem? _currentArmor;

    // Torso model ranges organized by armor class (AC)
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

    // Valid model indices for each armor part
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
        // Don't load armor on construction - only when Armor mode is selected
    }

    /// <summary>
    /// Load current armor item
    /// </summary>
    private void LoadCurrentArmor()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        _currentArmor = creature.GetItemInSlot(InventorySlot.Chest);
    }

    public void SetMode(CustomizationMode mode)
    {
        CurrentMode = mode;
        CurrentArmorPart = 0; // Reset to first part when switching modes

        // Send message when entering Armor mode
        if (mode == CustomizationMode.Armor)
        {
            LoadCurrentArmor();
            if (_currentArmor != null && _currentArmor.IsValid)
            {
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
        if (partIndex >= 0 && partIndex < 19)
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
        if (_currentArmor == null || !_currentArmor.IsValid)
        {
            _player.SendServerMessage("No armor equipped to modify.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        // Map our part index to CreaturePart enum
        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);

        int currentModel = _currentArmor.Appearance.GetArmorModel(creaturePart);
        int newModel = currentModel + delta;

        // Apply range restrictions for Torso part only - check AC and keep within valid models
        if (creaturePart == CreaturePart.Torso)
        {
            // Determine which AC range the current model belongs to
            int? currentAc = GetArmorClassFromTorsoModel(currentModel);

            if (currentAc.HasValue && TorsoModelsByAc.TryGetValue(currentAc.Value, out HashSet<int>? validModels))
            {
                // Convert to sorted list for cycling
                List<int> sortedModels = validModels.OrderBy(m => m).ToList();
                int currentIndex = sortedModels.IndexOf(currentModel);

                if (currentIndex >= 0)
                {
                    // For small deltas (+1/-1), move by index position and wrap around
                    // For large deltas (+10/-10), find closest model to currentModel ± delta
                    if (Math.Abs(delta) == 1)
                    {
                        // Move by index position and wrap around
                        int newIndex = currentIndex + delta;
                        if (newIndex < 0)
                            newIndex = sortedModels.Count - 1;
                        else if (newIndex >= sortedModels.Count)
                            newIndex = 0;

                        newModel = sortedModels[newIndex];
                    }
                    else
                    {
                        // For +10/-10, find the closest valid model to (currentModel + delta)
                        int targetModel = currentModel + delta;

                        if (delta > 0)
                        {
                            // Going forward: find first model >= targetModel
                            newModel = sortedModels.FirstOrDefault(m => m >= targetModel);
                            // If none found, wrap to first
                            if (newModel == 0)
                                newModel = sortedModels.First();
                        }
                        else
                        {
                            // Going backward: find last model <= targetModel
                            newModel = sortedModels.LastOrDefault(m => m <= targetModel);
                            // If none found, wrap to last
                            if (newModel == 0)
                                newModel = sortedModels.Last();
                        }
                    }
                }
                else
                {
                    // Current model not found in valid list, default to first valid model
                    newModel = sortedModels.FirstOrDefault();
                }
            }
            else
            {
                // Unknown AC or no valid models, keep current model
                _player.SendServerMessage("Could not determine armor class. No change made.", ColorConstants.Orange);
                return;
            }
        }
        else
        {
            // For other parts, use the valid models dictionary
            if (ValidModelsByPart.TryGetValue(creaturePart, out HashSet<int>? validModels))
            {
                // Convert to sorted list for cycling
                List<int> sortedModels = validModels.OrderBy(m => m).ToList();
                int currentIndex = sortedModels.IndexOf(currentModel);

                if (currentIndex >= 0)
                {
                    // For small deltas (+1/-1), move by index position and wrap around
                    // For large deltas (+10/-10), find closest model to currentModel ± delta
                    if (Math.Abs(delta) == 1)
                    {
                        // Move by index position and wrap around
                        int newIndex = currentIndex + delta;
                        if (newIndex < 0)
                            newIndex = sortedModels.Count - 1;
                        else if (newIndex >= sortedModels.Count)
                            newIndex = 0;

                        newModel = sortedModels[newIndex];
                    }
                    else
                    {
                        // For +10/-10, find the closest valid model to (currentModel + delta)
                        int targetModel = currentModel + delta;

                        if (delta > 0)
                        {
                            // Going forward: find first model >= targetModel
                            newModel = sortedModels.FirstOrDefault(m => m >= targetModel);
                            // If none found, wrap to first
                            if (newModel == 0)
                                newModel = sortedModels.First();
                        }
                        else
                        {
                            // Going backward: find last model <= targetModel
                            newModel = sortedModels.LastOrDefault(m => m <= targetModel);
                            // If none found, wrap to last
                            if (newModel == 0)
                                newModel = sortedModels.Last();
                        }
                    }
                }
                else
                {
                    // Current model not found in valid list, default to first valid model
                    newModel = sortedModels.FirstOrDefault();
                }
            }
            else
            {
                // No valid models dictionary for this part, keep current model
                _player.SendServerMessage("No valid models for this part.", ColorConstants.Orange);
                return;
            }
        }


        // Store reference to old armor
        NwItem oldArmor = _currentArmor;

        // Use Anvil's native method to directly modify the armor appearance
        oldArmor.Appearance.SetArmorModel(creaturePart, (byte)newModel);

        // Unequip the armor
        creature.RunUnequip(oldArmor);

        // Create a copy to refresh the inventory icon
        NwItem? newArmor = oldArmor.Clone(creature);

        if (newArmor == null || !newArmor.IsValid)
        {
            _player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            // Re-equip old armor
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        // Re-equip the new copy
        creature.RunEquip(newArmor, InventorySlot.Chest);

        // Update our reference
        _currentArmor = newArmor;

        // Destroy the old armor
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

        // Map color channel to ItemAppearanceArmorColor enum
        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentColorChannel);

        string[] channelNames = new[] { "Leather 1", "Leather 2", "Cloth 1", "Cloth 2", "Metal 1", "Metal 2" };
        string channelName = CurrentColorChannel < channelNames.Length ? channelNames[CurrentColorChannel] : "Unknown";


        // Store reference to old armor
        NwItem oldArmor = _currentArmor;

        // Use Anvil's native method to directly modify the armor color
        oldArmor.Appearance.SetArmorColor(colorChannel, (byte)colorIndex);

        // Unequip the armor
        creature.RunUnequip(oldArmor);

        // Create a copy to refresh the inventory icon
        NwItem? newArmor = oldArmor.Clone(creature);

        if (newArmor == null || !newArmor.IsValid)
        {
            _player.SendServerMessage("Failed to refresh armor.", ColorConstants.Red);
            // Re-equip old armor
            creature.RunEquip(oldArmor, InventorySlot.Chest);
            return;
        }

        // Re-equip the new copy
        creature.RunEquip(newArmor, InventorySlot.Chest);

        // Update our reference
        _currentArmor = newArmor;

        // Destroy the old armor
        oldArmor.Destroy();

        _player.SendServerMessage($"{channelName} color updated to {colorIndex}.", ColorConstants.Green);
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

        CreaturePart creaturePart = GetCreaturePart(CurrentArmorPart);
        return _currentArmor.Appearance.GetArmorModel(creaturePart);
    }

    public int GetCurrentArmorPartColor()
    {
        if (_currentArmor == null || !_currentArmor.IsValid) return 0;

        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentColorChannel);
        return _currentArmor.Appearance.GetArmorColor(colorChannel);
    }

    /// <summary>
    /// Map our part index to Anvil's CreaturePart enum
    /// </summary>
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

    /// <summary>
    /// Map our color channel index to Anvil's ItemAppearanceArmorColor enum
    /// </summary>
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

    /// <summary>
    /// Determine which AC range a torso model belongs to
    /// </summary>
    private int? GetArmorClassFromTorsoModel(int modelIndex)
    {
        foreach (var kvp in TorsoModelsByAc)
        {
            if (kvp.Value.Contains(modelIndex))
            {
                return kvp.Key;
            }
        }
        return null; // Model not found in any AC range
    }


    /// <summary>
    /// Apply all changes to the character (for now, changes are applied immediately)
    /// </summary>
    public void ApplyChanges()
    {
        _player.SendServerMessage("Character appearance saved!", ColorConstants.Green);
    }

    /// <summary>
    /// Revert all changes back to original (not implemented yet - would need to store original)
    /// </summary>
    public void RevertChanges()
    {
        _player.SendServerMessage("Revert feature coming soon!", ColorConstants.Yellow);
        LoadCurrentArmor();
    }
}

public enum CustomizationMode
{
    None,
    Armor,
    Equipment,
    Appearance
}

