using Anvil;
using Anvil.API;
using Anvil.Services;
using Newtonsoft.Json;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public enum EquipmentType
{
    None,
    Weapon,
    Boots,
    Helmet,
    Cloak
}

public sealed class EquipmentCustomizationModel(NwPlayer player)
{
    private const string BackupDataKey = "EQUIPMENT_CUSTOMIZATION_BACKUP";

    public EquipmentType CurrentEquipmentType { get; private set; } = EquipmentType.None;

    // Current equipped items
    private NwItem? _currentWeapon;
    private NwItem? _currentBoots;
    private NwItem? _currentHelmet;
    private NwItem? _currentCloak;

    // Weapon
    public int WeaponTopModel { get; private set; } = 1;
    public int WeaponMidModel { get; private set; } = 1;
    public int WeaponBotModel { get; private set; } = 1;

    private int _weaponTopModelMax = 255;
    private int _weaponMidModelMax = 255;
    private int _weaponBotModelMax = 255;

    // Boots
    public int BootsTopModel { get; private set; } = 1;
    public int BootsMidModel { get; private set; } = 1;
    public int BootsBotModel { get; private set; } = 1;
    public int BootsTopColor { get; private set; } = 1;
    public int BootsMidColor { get; private set; } = 1;
    public int BootsBotColor { get; private set; } = 1;

    // Helmet
    public int HelmetAppearance { get; private set; } = 1;

    // Cloak
    public int CloakAppearance { get; private set; } = 1;

    public void SelectEquipmentType(EquipmentType type)
    {
        CurrentEquipmentType = type;

        switch (type)
        {
            case EquipmentType.Weapon:
                LoadWeaponData();
                break;
            case EquipmentType.Boots:
                LoadBootsData();
                break;
            case EquipmentType.Helmet:
                LoadHelmetData();
                break;
            case EquipmentType.Cloak:
                LoadCloakData();
                break;
        }
    }

    private void LoadWeaponData()
    {
        NwItem? weapon = player.ControlledCreature?.GetItemInSlot(InventorySlot.RightHand);
        if (weapon != null && weapon.IsValid)
        {
            _currentWeapon = weapon;
            player.SendServerMessage($"Selected weapon: {weapon.Name}", ColorConstants.Cyan);

            // Get model range from BaseItem (applies to Top, Middle, and Bottom)
            int modelRange = (int)weapon.BaseItem.ModelRangeMax;
            _weaponTopModelMax = modelRange;
            _weaponMidModelMax = modelRange;
            _weaponBotModelMax = modelRange;

            // Load current weapon appearance
            WeaponTopModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            WeaponMidModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            WeaponBotModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);

            // Save backup on first selection
            SaveBackupToPcKey();
        }
        else
        {
            player.SendServerMessage("No weapon equipped in main hand.", ColorConstants.Orange);
        }
    }

    private void LoadBootsData()
    {
        NwItem? boots = player.ControlledCreature?.GetItemInSlot(InventorySlot.Boots);
        if (boots != null && boots.IsValid)
        {
            _currentBoots = boots;
            player.SendServerMessage($"Selected boots: {boots.Name}", ColorConstants.Cyan);
            // TODO: Load boots appearance
            SaveBackupToPcKey();
        }
        else
        {
            player.SendServerMessage("No boots equipped.", ColorConstants.Orange);
        }
    }

    private void LoadHelmetData()
    {
        NwItem? helmet = player.ControlledCreature?.GetItemInSlot(InventorySlot.Head);
        if (helmet != null && helmet.IsValid)
        {
            _currentHelmet = helmet;
            player.SendServerMessage($"Selected helmet: {helmet.Name}", ColorConstants.Cyan);
            // TODO: Load helmet appearance
            SaveBackupToPcKey();
        }
        else
        {
            player.SendServerMessage("No helmet equipped.", ColorConstants.Orange);
        }
    }

    private void LoadCloakData()
    {
        NwItem? cloak = player.ControlledCreature?.GetItemInSlot(InventorySlot.Cloak);
        if (cloak != null && cloak.IsValid)
        {
            _currentCloak = cloak;
            player.SendServerMessage($"Selected cloak: {cloak.Name}", ColorConstants.Cyan);
            // TODO: Load cloak appearance
            SaveBackupToPcKey();
        }
        else
        {
            player.SendServerMessage("No cloak equipped.", ColorConstants.Orange);
        }
    }

    public void AdjustWeaponTopModel(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        WeaponTopModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Top, WeaponTopModel, delta, _weaponTopModelMax);
        ApplyWeaponChanges();
        player.SendServerMessage($"Weapon top model set to {WeaponTopModel}.", ColorConstants.Green);
    }

    public void AdjustWeaponMidModel(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        WeaponMidModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Middle, WeaponMidModel, delta, _weaponMidModelMax);
        ApplyWeaponChanges();
        player.SendServerMessage($"Weapon middle model set to {WeaponMidModel}.", ColorConstants.Green);
    }

    public void AdjustWeaponBotModel(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        WeaponBotModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Bottom, WeaponBotModel, delta, _weaponBotModelMax);
        ApplyWeaponChanges();
        player.SendServerMessage($"Weapon bottom model set to {WeaponBotModel}.", ColorConstants.Green);
    }

    private int GetNextValidWeaponModel(ItemAppearanceWeaponModel modelPart, int currentModel, int delta, int maxModel)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return currentModel;
        if (maxModel <= 0) return currentModel;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return currentModel;

        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchModel = currentModel;
        int attemptsRemaining = maxModel + 1; // Prevent infinite loops

        while (attemptsRemaining > 0)
        {
            // Calculate next model to test
            if (step == 1)
            {
                searchModel += direction;
            }
            else // step >= 10
            {
                searchModel += delta;
                step = 1; // After first jump, continue single steps
            }

            // Handle wraparound
            if (searchModel > maxModel)
            {
                searchModel = 1;
            }
            else if (searchModel < 1)
            {
                searchModel = maxModel;
            }

            // If we've wrapped back to starting model, we've tried everything
            if (searchModel == currentModel && attemptsRemaining < maxModel)
            {
                player.SendServerMessage("No other valid models found.", ColorConstants.Orange);
                return currentModel;
            }

            // Test if this model is valid (not bag model)
            if (IsValidWeaponModel(modelPart, searchModel, creature))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid weapon model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidWeaponModel(ItemAppearanceWeaponModel modelPart, int modelNumber, NwCreature creature)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return false;

        // Model 0 is always invalid
        if (modelNumber == 0) return false;

        // Get the ItemClass prefix from the BaseItem (e.g., "wswss" for short sword)
        string itemClass = _currentWeapon.BaseItem.ItemClass;

        if (string.IsNullOrEmpty(itemClass)) return false;

        // Determine which part letter to use
        string partLetter = modelPart switch
        {
            ItemAppearanceWeaponModel.Top => "t",
            ItemAppearanceWeaponModel.Middle => "m",
            ItemAppearanceWeaponModel.Bottom => "b",
            _ => ""
        };

        // Build the model filename: itemclass_PART_###
        // Example: wswss_t_022 for shortsword top model 22
        string modelResRef = $"{itemClass}_{partLetter}_{modelNumber:D3}";

        // Use NWScript to check if the resource exists (ResManGetAliasFor returns empty string if not found)
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);


        // If alias is not empty, the resource exists
        return !string.IsNullOrEmpty(alias);
    }

    private void ApplyWeaponChanges()
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        NwItem oldWeapon = _currentWeapon;

        // Set weapon appearance
        oldWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)WeaponTopModel);
        oldWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)WeaponMidModel);
        oldWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)WeaponBotModel);


        // Unequip, clone, and re-equip to refresh appearance
        creature.RunUnequip(oldWeapon);
        NwItem newWeapon = oldWeapon.Clone(creature);

        if (!newWeapon.IsValid)
        {
            player.SendServerMessage("Failed to refresh weapon.", ColorConstants.Red);
            creature.RunEquip(oldWeapon, InventorySlot.RightHand);
            return;
        }

        creature.RunEquip(newWeapon, InventorySlot.RightHand);
        _currentWeapon = newWeapon;
        oldWeapon.Destroy();
    }

    public void AdjustBootsTopModel(int delta)
    {
        BootsTopModel = Math.Clamp(BootsTopModel + delta, 1, 4);
    }

    public void AdjustBootsMidModel(int delta)
    {
        BootsMidModel = Math.Clamp(BootsMidModel + delta, 1, 4);
    }

    public void AdjustBootsBotModel(int delta)
    {
        BootsBotModel = Math.Clamp(BootsBotModel + delta, 1, 4);
    }

    public void AdjustBootsTopColor(int delta)
    {
        BootsTopColor = Math.Clamp(BootsTopColor + delta, 1, 4);
    }

    public void AdjustBootsMidColor(int delta)
    {
        BootsMidColor = Math.Clamp(BootsMidColor + delta, 1, 4);
    }

    public void AdjustBootsBotColor(int delta)
    {
        BootsBotColor = Math.Clamp(BootsBotColor + delta, 1, 4);
    }

    public void AdjustHelmetAppearance(int delta)
    {
        HelmetAppearance = Math.Clamp(HelmetAppearance + delta, 1, 20);
    }

    public void AdjustCloakAppearance(int delta)
    {
        CloakAppearance = Math.Clamp(CloakAppearance + delta, 1, 20);
    }

    public void ApplyChanges()
    {
        SaveBackupToPcKey();
        player.SendServerMessage(
            "Equipment customization saved! You can continue editing or click Revert to return to this save point.",
            ColorConstants.Green);
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

        // Revert weapon
        if (CurrentEquipmentType == EquipmentType.Weapon && _currentWeapon != null && _currentWeapon.IsValid)
        {
            creature.RunUnequip(_currentWeapon);
            NwItem newWeapon = _currentWeapon.Clone(creature);

            if (newWeapon.IsValid && backupData.WeaponData != null)
            {
                backupData.WeaponData.ApplyToItem(newWeapon);
                creature.RunEquip(newWeapon, InventorySlot.RightHand);
                _currentWeapon.Destroy();
                _currentWeapon = newWeapon;

                // Update model values
                WeaponTopModel = backupData.WeaponData.TopModel;
                WeaponMidModel = backupData.WeaponData.MidModel;
                WeaponBotModel = backupData.WeaponData.BotModel;

                player.SendServerMessage("Weapon customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                creature.RunEquip(_currentWeapon, InventorySlot.RightHand);
                player.SendServerMessage("Failed to revert weapon changes.", ColorConstants.Red);
            }
        }

        // TODO: Implement revert for other equipment types
    }

    private void SaveBackupToPcKey()
    {
        var backupData = new EquipmentBackupData();

        // Save weapon data
        if (_currentWeapon != null && _currentWeapon.IsValid)
        {
            backupData.WeaponData = WeaponBackupData.FromItem(_currentWeapon);
        }

        // TODO: Save other equipment types

        string json = JsonConvert.SerializeObject(backupData);

        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.SetLocalString(pcKey, BackupDataKey, json);
        }
    }

    private EquipmentBackupData? LoadBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid)
        {
            return null;
        }

        string json = NWScript.GetLocalString(pcKey, BackupDataKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<EquipmentBackupData>(json);
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

public class EquipmentBackupData
{
    public WeaponBackupData? WeaponData { get; set; }
    // TODO: Add other equipment types
}

public class WeaponBackupData
{
    public int TopModel { get; set; }
    public int MidModel { get; set; }
    public int BotModel { get; set; }

    public static WeaponBackupData FromItem(NwItem weapon)
    {
        return new WeaponBackupData
        {
            TopModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top),
            MidModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle),
            BotModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom)
        };
    }

    public void ApplyToItem(NwItem weapon)
    {
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)TopModel);
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)MidModel);
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BotModel);
    }
}

