using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

/// <summary>
/// Model for character customization - handles business logic for appearance changes
/// </summary>
public sealed class CharacterCustomizationModel
{
    private readonly NwPlayer _player;

    // Current customization mode
    public CustomizationMode CurrentMode { get; private set; } = CustomizationMode.Armor;

    // Current armor part being edited (for armor mode)
    public int CurrentArmorPart { get; private set; } = 0;

    // Current color being edited
    public int CurrentColor { get; private set; } = 0;

    // Temporary storage for changes (not applied until Save)
    private readonly Dictionary<int, int> _tempArmorPartModels = new();
    private readonly Dictionary<int, int> _tempArmorPartColors = new();
    private NwItem? _tempHelmet;
    private NwItem? _tempCloak;
    private NwItem? _tempWeaponRight;
    private NwItem? _tempWeaponLeft;
    private NwItem? _tempShield;
    private NwItem? _tempBoots;
    private int? _tempHairColor;
    private int? _tempTattoo1Color;
    private int? _tempTattoo2Color;

    public CharacterCustomizationModel(NwPlayer player)
    {
        _player = player;
        LoadCurrentAppearance();
    }

    /// <summary>
    /// Load current character appearance into temporary storage
    /// </summary>
    private void LoadCurrentAppearance()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        // Load current armor appearance
        NwItem? armor = creature.GetItemInSlot(InventorySlot.Chest);
        if (armor != null)
        {
            foreach (CreaturePart part in Enum.GetValues<CreaturePart>())
            {
                _tempArmorPartModels[(int)part] = armor.Appearance.GetArmorModel(part);
                // Note: Color per part not directly available, using armor color channels instead
            }

            foreach (ItemAppearanceArmorColor colorChannel in Enum.GetValues<ItemAppearanceArmorColor>())
            {
                _tempArmorPartColors[(int)colorChannel] = armor.Appearance.GetArmorColor(colorChannel);
            }
        }

        // Load equipment references
        _tempHelmet = creature.GetItemInSlot(InventorySlot.Head);
        _tempCloak = creature.GetItemInSlot(InventorySlot.Cloak);
        _tempWeaponRight = creature.GetItemInSlot(InventorySlot.RightHand);
        _tempWeaponLeft = creature.GetItemInSlot(InventorySlot.LeftHand);
        _tempBoots = creature.GetItemInSlot(InventorySlot.Boots);

        // Shield could be in either hand - check by base item category
        if (_tempWeaponLeft?.BaseItem.Category == BaseItemCategory.Shield)
        {
            _tempShield = _tempWeaponLeft;
            _tempWeaponLeft = null;
        }
        else if (_tempWeaponRight?.BaseItem.Category == BaseItemCategory.Shield)
        {
            _tempShield = _tempWeaponRight;
            _tempWeaponRight = null;
        }

        // Load appearance colors - store as placeholders for now
        // Note: NwCreature color API may need adjustment based on actual Anvil implementation
        _tempHairColor = 0; // Placeholder
        _tempTattoo1Color = 0; // Placeholder
        _tempTattoo2Color = 0; // Placeholder
    }

    public void SetMode(CustomizationMode mode)
    {
        CurrentMode = mode;
        CurrentArmorPart = 0; // Reset to first part when switching modes
    }

    public void SetArmorPart(int partIndex)
    {
        if (partIndex >= 0 && partIndex < 19)
        {
            CurrentArmorPart = partIndex;
        }
    }

    public void AdjustArmorPartModel(int delta)
    {
        if (CurrentMode != CustomizationMode.Armor) return;

        int currentModel = _tempArmorPartModels.GetValueOrDefault(CurrentArmorPart, 0);
        int newModel = Math.Max(0, currentModel + delta);
        _tempArmorPartModels[CurrentArmorPart] = newModel;
    }

    public void SetArmorPartColor(int colorIndex)
    {
        if (CurrentMode != CustomizationMode.Armor) return;
        if (colorIndex < 0 || colorIndex > 175) return;

        _tempArmorPartColors[CurrentArmorPart] = colorIndex;
        CurrentColor = colorIndex;
    }

    public void SetHairColor(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _tempHairColor = colorIndex;
    }

    public void SetTattoo1Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _tempTattoo1Color = colorIndex;
    }

    public void SetTattoo2Color(int colorIndex)
    {
        if (colorIndex < 0 || colorIndex > 175) return;
        _tempTattoo2Color = colorIndex;
    }

    public int GetCurrentArmorPartModel()
    {
        return _tempArmorPartModels.GetValueOrDefault(CurrentArmorPart, 0);
    }

    public int GetCurrentArmorPartColor()
    {
        return _tempArmorPartColors.GetValueOrDefault(CurrentArmorPart, 0);
    }

    /// <summary>
    /// Apply all changes to the character
    /// </summary>
    public void ApplyChanges()
    {
        NwCreature? creature = _player.ControlledCreature;
        if (creature == null) return;

        // Apply armor appearance changes
        NwItem? armor = creature.GetItemInSlot(InventorySlot.Chest);
        if (armor != null)
        {
            foreach ((int partIndex, int model) in _tempArmorPartModels)
            {
                if (Enum.IsDefined(typeof(CreaturePart), partIndex))
                {
                    armor.Appearance.SetArmorModel((CreaturePart)partIndex, (byte)model);
                }
            }

            foreach ((int colorIndex, int color) in _tempArmorPartColors)
            {
                if (Enum.IsDefined(typeof(ItemAppearanceArmorColor), colorIndex))
                {
                    armor.Appearance.SetArmorColor((ItemAppearanceArmorColor)colorIndex, (byte)color);
                }
            }
        }

        // Apply appearance colors - TODO: Implement when creature color API is confirmed
        if (_tempHairColor.HasValue)
        {
            // creature.VisualTransform or creature appearance color setting here
            _player.SendServerMessage($"Hair color {_tempHairColor.Value} - feature pending API confirmation", ColorConstants.Yellow);
        }
        if (_tempTattoo1Color.HasValue)
        {
            _player.SendServerMessage($"Tattoo1 color {_tempTattoo1Color.Value} - feature pending API confirmation", ColorConstants.Yellow);
        }
        if (_tempTattoo2Color.HasValue)
        {
            _player.SendServerMessage($"Tattoo2 color {_tempTattoo2Color.Value} - feature pending API confirmation", ColorConstants.Yellow);
        }

        _player.SendServerMessage("Character appearance updated!", ColorConstants.Green);
    }

    /// <summary>
    /// Revert all changes back to original
    /// </summary>
    public void RevertChanges()
    {
        _tempArmorPartModels.Clear();
        _tempArmorPartColors.Clear();
        _tempHairColor = null;
        _tempTattoo1Color = null;
        _tempTattoo2Color = null;
        LoadCurrentAppearance();
    }
}

public enum CustomizationMode
{
    Armor,
    Equipment,
    Appearance
}

