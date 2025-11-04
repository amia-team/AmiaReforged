using Anvil.API;
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

    private NwItem? _currentWeapon;
    private NwItem? _currentBoots;
    private NwItem? _currentHelmet;
    private NwItem? _currentCloak;

    public int WeaponTopModel { get; private set; } = 1;
    public int WeaponMidModel { get; private set; } = 1;
    public int WeaponBotModel { get; private set; } = 1;
    public int WeaponScale { get; private set; } = 100;

    private const int MinWeaponScale = 80;
    private const int MaxWeaponScale = 120;

    private bool _onlyScaleChanged;

    private int _weaponTopModelMax = 255;
    private int _weaponMidModelMax = 255;
    private int _weaponBotModelMax = 255;

    public int BootsTopModel { get; private set; } = 1;
    public int BootsMidModel { get; private set; } = 1;
    public int BootsBotModel { get; private set; } = 1;

    private int _bootsTopModelMax = 255;
    private int _bootsMidModelMax = 255;
    private int _bootsBotModelMax = 255;

    public int HelmetAppearance { get; private set; } = 1;
    private int _helmetAppearanceMax = 255;
    private int CurrentHelmetColorChannel { get; set; } = 2;

    public int CloakAppearance { get; private set; } = 1;
    private int _cloakAppearanceMax = 255;
    private int CurrentCloakColorChannel { get; set; } = 2;

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
            player.SendServerMessage($"Selected main hand item: {weapon.Name}", ColorConstants.Cyan);

            int modelRange = (int)weapon.BaseItem.ModelRangeMax;
            _weaponTopModelMax = modelRange;
            _weaponMidModelMax = modelRange;
            _weaponBotModelMax = modelRange;

            WeaponTopModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            WeaponMidModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            WeaponBotModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);

            VisualTransform transform = weapon.VisualTransform;
            WeaponScale = (int)(transform.Scale * 100);

            SaveBackupToPcKey();
        }
        else
        {
            player.SendServerMessage("Nothing equipped in main hand.", ColorConstants.Orange);
        }
    }

    private void LoadBootsData()
    {
        NwItem? boots = player.ControlledCreature?.GetItemInSlot(InventorySlot.Boots);
        if (boots != null && boots.IsValid)
        {
            _currentBoots = boots;
            player.SendServerMessage($"Selected boots: {boots.Name}", ColorConstants.Cyan);

            int modelRange = (int)boots.BaseItem.ModelRangeMax;
            _bootsTopModelMax = modelRange;
            _bootsMidModelMax = modelRange;
            _bootsBotModelMax = modelRange;

            BootsTopModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            BootsMidModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            BootsBotModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);

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
            _helmetAppearanceMax = (int)helmet.BaseItem.ModelRangeMax;
            HelmetAppearance = NWScript.GetItemAppearance(helmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
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
            _cloakAppearanceMax = 86;
            CloakAppearance = NWScript.GetItemAppearance(cloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
            player.SendServerMessage($"Current cloak appearance: {CloakAppearance}", ColorConstants.Gray);
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

        _onlyScaleChanged = false;
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

        _onlyScaleChanged = false;
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

        _onlyScaleChanged = false;
        WeaponBotModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Bottom, WeaponBotModel, delta, _weaponBotModelMax);
        ApplyWeaponChanges();
        player.SendServerMessage($"Weapon bottom model set to {WeaponBotModel}.", ColorConstants.Green);
    }

    public void AdjustWeaponScale(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        int newScale = WeaponScale + delta;

        if (newScale < MinWeaponScale)
        {
            player.SendServerMessage($"This item cannot be scaled lower than {MinWeaponScale}%.", ColorConstants.Orange);
            return;
        }

        if (newScale > MaxWeaponScale)
        {
            player.SendServerMessage($"This item cannot be scaled higher than {MaxWeaponScale}%.", ColorConstants.Orange);
            return;
        }

        _onlyScaleChanged = true;
        WeaponScale = newScale;
        ApplyWeaponChanges();

        int percentDiff = WeaponScale - 100;
        string sign = percentDiff > 0 ? "+" : "";
        player.SendServerMessage($"Main hand scale set to {WeaponScale}% ({sign}{percentDiff}%).", ColorConstants.Green);
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

            if (IsValidWeaponModel(modelPart, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid weapon model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidWeaponModel(ItemAppearanceWeaponModel modelPart, int modelNumber)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return false;

        if (modelNumber == 0) return false;

        string itemClass = _currentWeapon.BaseItem.ItemClass;

        if (string.IsNullOrEmpty(itemClass)) return false;

        string partLetter = modelPart switch
        {
            ItemAppearanceWeaponModel.Top => "t",
            ItemAppearanceWeaponModel.Middle => "m",
            ItemAppearanceWeaponModel.Bottom => "b",
            _ => ""
        };

        string modelResRef = $"{itemClass}_{partLetter}_{modelNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);
        return !string.IsNullOrEmpty(alias);
    }

    private void ApplyWeaponChanges()
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;
        NwItem currentWeapon = _currentWeapon;
        float scaleValue = WeaponScale / 100f;
        NWScript.SetObjectVisualTransform(currentWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

        if (_onlyScaleChanged)
        {
            return;
        }

        currentWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)WeaponTopModel);
        currentWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)WeaponMidModel);
        currentWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)WeaponBotModel);

        creature.RunUnequip(currentWeapon);
        NwItem newWeapon = currentWeapon.Clone(creature);

        if (!newWeapon.IsValid)
        {
            player.SendServerMessage("Failed to refresh weapon.", ColorConstants.Red);
            creature.RunEquip(currentWeapon, InventorySlot.RightHand);
            return;
        }

        creature.RunEquip(newWeapon, InventorySlot.RightHand);
        NWScript.SetObjectVisualTransform(newWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
        _currentWeapon = newWeapon;
        currentWeapon.Destroy();
    }

    private int GetNextValidBootsModel(ItemAppearanceWeaponModel modelPart, int currentModel, int delta, int maxModel)
    {
        if (_currentBoots == null || !_currentBoots.IsValid) return currentModel;
        if (maxModel <= 0) return currentModel;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return currentModel;

        int direction = Math.Sign(delta);
        int step = Math.Abs(delta);
        int searchModel = currentModel;
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

            if (IsValidBootsModel(modelPart, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid boots model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidBootsModel(ItemAppearanceWeaponModel modelPart, int modelNumber)
    {
        if (_currentBoots == null || !_currentBoots.IsValid) return false;

        if (modelNumber == 0) return false;

        string itemClass = "iit_boots";

        string partLetter = modelPart switch
        {
            ItemAppearanceWeaponModel.Top => "t",
            ItemAppearanceWeaponModel.Middle => "m",
            ItemAppearanceWeaponModel.Bottom => "b",
            _ => ""
        };

        string modelResRef = $"{itemClass}_{partLetter}_{modelNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_TGA);
        return !string.IsNullOrEmpty(alias);
    }

    private void ApplyBootsChanges()
    {
        if (_currentBoots == null || !_currentBoots.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        NwItem oldBoots = _currentBoots;

        oldBoots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)BootsTopModel);
        oldBoots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)BootsMidModel);
        oldBoots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BootsBotModel);

        creature.RunUnequip(oldBoots);
        NwItem newBoots = oldBoots.Clone(creature);

        if (!newBoots.IsValid)
        {
            player.SendServerMessage("Failed to refresh boots.", ColorConstants.Red);
            creature.RunEquip(oldBoots, InventorySlot.Boots);
            return;
        }

        creature.RunEquip(newBoots, InventorySlot.Boots);
        _currentBoots = newBoots;
        oldBoots.Destroy();
    }

    public void AdjustBootsTopModel(int delta)
    {
        if (_currentBoots == null || !_currentBoots.IsValid)
        {
            player.SendServerMessage("No boots selected.", ColorConstants.Orange);
            return;
        }

        BootsTopModel = GetNextValidBootsModel(ItemAppearanceWeaponModel.Top, BootsTopModel, delta, _bootsTopModelMax);
        ApplyBootsChanges();
        player.SendServerMessage($"Boots top model set to {BootsTopModel}.", ColorConstants.Green);
    }

    public void AdjustBootsMidModel(int delta)
    {
        if (_currentBoots == null || !_currentBoots.IsValid)
        {
            player.SendServerMessage("No boots selected.", ColorConstants.Orange);
            return;
        }

        BootsMidModel = GetNextValidBootsModel(ItemAppearanceWeaponModel.Middle, BootsMidModel, delta, _bootsMidModelMax);
        ApplyBootsChanges();
        player.SendServerMessage($"Boots middle model set to {BootsMidModel}.", ColorConstants.Green);
    }

    public void AdjustBootsBotModel(int delta)
    {
        if (_currentBoots == null || !_currentBoots.IsValid)
        {
            player.SendServerMessage("No boots selected.", ColorConstants.Orange);
            return;
        }

        BootsBotModel = GetNextValidBootsModel(ItemAppearanceWeaponModel.Bottom, BootsBotModel, delta, _bootsBotModelMax);
        ApplyBootsChanges();
        player.SendServerMessage($"Boots bottom model set to {BootsBotModel}.", ColorConstants.Green);
    }

    public void AdjustHelmetAppearance(int delta)
    {
        if (_currentHelmet == null || !_currentHelmet.IsValid)
        {
            player.SendServerMessage("No helmet selected.", ColorConstants.Orange);
            return;
        }

        HelmetAppearance = GetNextValidHelmetAppearance(HelmetAppearance, delta, _helmetAppearanceMax);
        ApplyHelmetChanges();
        player.SendServerMessage($"Helmet appearance set to {HelmetAppearance}.", ColorConstants.Green);
    }

    private int GetNextValidHelmetAppearance(int currentAppearance, int delta, int maxAppearance)
    {
        if (_currentHelmet == null || !_currentHelmet.IsValid) return currentAppearance;
        if (maxAppearance <= 0) return currentAppearance;

        int direction = Math.Sign(delta);
        int searchAppearance = currentAppearance;
        int attemptsRemaining = maxAppearance + 1;

        while (attemptsRemaining > 0)
        {
            searchAppearance += direction;

            if (searchAppearance > maxAppearance)
            {
                searchAppearance = 1;
            }
            else if (searchAppearance < 1)
            {
                searchAppearance = maxAppearance;
            }

            if (searchAppearance == currentAppearance && attemptsRemaining < maxAppearance)
            {
                player.SendServerMessage("No other valid appearances found.", ColorConstants.Orange);
                return currentAppearance;
            }

            if (IsValidHelmetAppearance(searchAppearance))
            {
                return searchAppearance;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid helmet appearance.", ColorConstants.Orange);
        return currentAppearance;
    }

    private bool IsValidHelmetAppearance(int appearanceNumber)
    {
        if (_currentHelmet == null || !_currentHelmet.IsValid) return false;
        if (appearanceNumber < 0) return false;

        string modelResRef = $"helm_{appearanceNumber:D3}";
        string alias = NWScript.ResManGetAliasFor(modelResRef, NWScript.RESTYPE_MDL);
        return !string.IsNullOrEmpty(alias);
    }

    private void ApplyHelmetChanges()
    {
        if (_currentHelmet == null || !_currentHelmet.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        uint copy = NWScript.CopyItemAndModify(_currentHelmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, HelmetAppearance, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            creature.RunUnequip(_currentHelmet);
            NWScript.DestroyObject(_currentHelmet);
            _currentHelmet = copy.ToNwObject<NwItem>();
            if (_currentHelmet != null) creature.RunEquip(_currentHelmet, InventorySlot.Head);
        }
        else
        {
            player.SendServerMessage("Failed to update helmet appearance.", ColorConstants.Red);
        }
    }

    public void AdjustCloakAppearance(int delta)
    {
        if (_currentCloak == null || !_currentCloak.IsValid)
        {
            player.SendServerMessage("No cloak selected.", ColorConstants.Orange);
            return;
        }

        CloakAppearance = GetNextValidCloakAppearance(CloakAppearance, delta, _cloakAppearanceMax);
        ApplyCloakChanges();
        player.SendServerMessage($"Cloak appearance set to {CloakAppearance}.", ColorConstants.Green);
    }

    private int GetNextValidCloakAppearance(int currentAppearance, int delta, int maxAppearance)
    {
        if (_currentCloak == null || !_currentCloak.IsValid) return currentAppearance;
        if (maxAppearance <= 0) return currentAppearance;

        int direction = Math.Sign(delta);
        int searchAppearance = currentAppearance;
        int attemptsRemaining = maxAppearance + 1;

        while (attemptsRemaining > 0)
        {
            searchAppearance += direction;

            if (searchAppearance > maxAppearance)
            {
                searchAppearance = 1;
            }
            else if (searchAppearance < 1)
            {
                searchAppearance = maxAppearance;
            }

            if (searchAppearance == currentAppearance && attemptsRemaining < maxAppearance)
            {
                player.SendServerMessage("No other valid appearances found.", ColorConstants.Orange);
                return currentAppearance;
            }

            if (IsValidCloakAppearance(searchAppearance))
            {
                return searchAppearance;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid cloak appearance.", ColorConstants.Orange);
        return currentAppearance;
    }

    private bool IsValidCloakAppearance(int appearanceNumber)
    {
        if (_currentCloak == null || !_currentCloak.IsValid) return false;
        if (appearanceNumber < 0 || appearanceNumber > 86) return false;

        uint testCopy = NWScript.CopyItemAndModify(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, appearanceNumber, 1);
        bool isValid = NWScript.GetIsObjectValid(testCopy) == 1;

        if (isValid)
        {
            NWScript.DestroyObject(testCopy);
        }

        return isValid;
    }

    private void ApplyCloakChanges()
    {
        if (_currentCloak == null || !_currentCloak.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        uint copy = NWScript.CopyItemAndModify(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, CloakAppearance, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            creature.RunUnequip(_currentCloak);
            NWScript.DestroyObject(_currentCloak);
            _currentCloak = copy.ToNwObject<NwItem>();
            if (_currentCloak != null) creature.RunEquip(_currentCloak, InventorySlot.Cloak);
        }
        else
        {
            player.SendServerMessage("Failed to update cloak appearance.", ColorConstants.Red);
        }
    }

    public void SetHelmetColorChannel(int channel)
    {
        if (channel is >= 0 and <= 5)
        {
            CurrentHelmetColorChannel = channel;
        }
    }

    public void SetCloakColorChannel(int channel)
    {
        if (channel is >= 0 and <= 5)
        {
            CurrentCloakColorChannel = channel;
        }
    }

    public void SetHelmetColor(int colorIndex)
    {
        if (_currentHelmet == null || !_currentHelmet.IsValid)
        {
            player.SendServerMessage("No helmet selected.", ColorConstants.Orange);
            return;
        }

        if (colorIndex < 0 || colorIndex > 175) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentHelmetColorChannel);
        string[] channelNames = ["Leather 1", "Leather 2", "Cloth 1", "Cloth 2", "Metal 1", "Metal 2"];
        string channelName = CurrentHelmetColorChannel < channelNames.Length ? channelNames[CurrentHelmetColorChannel] : "Unknown";
        int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + CurrentHelmetColorChannel;
        uint copy = NWScript.CopyItemAndModify(_currentHelmet, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, colorIndex, 1);

        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            creature.RunUnequip(_currentHelmet);
            NWScript.DestroyObject(_currentHelmet);
            _currentHelmet = copy.ToNwObject<NwItem>();
            if (_currentHelmet != null) creature.RunEquip(_currentHelmet, InventorySlot.Head);
            player.SendServerMessage($"Helmet {channelName} color updated to {colorIndex}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to update helmet color.", ColorConstants.Red);
        }
    }

    public void SetCloakColor(int colorIndex)
    {
        if (_currentCloak == null || !_currentCloak.IsValid)
        {
            player.SendServerMessage("No cloak selected.", ColorConstants.Orange);
            return;
        }

        if (colorIndex < 0 || colorIndex > 175) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        ItemAppearanceArmorColor colorChannel = GetArmorColorChannel(CurrentCloakColorChannel);
        string[] channelNames = ["Leather 1", "Leather 2", "Cloth 1", "Cloth 2", "Metal 1", "Metal 2"];
        string channelName = CurrentCloakColorChannel < channelNames.Length ? channelNames[CurrentCloakColorChannel] : "Unknown";
        int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + CurrentCloakColorChannel;
        uint copy = NWScript.CopyItemAndModify(_currentCloak, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, colorIndex, 1);

        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            creature.RunUnequip(_currentCloak);
            NWScript.DestroyObject(_currentCloak);
            _currentCloak = copy.ToNwObject<NwItem>();
            if (_currentCloak != null) creature.RunEquip(_currentCloak, InventorySlot.Cloak);
            player.SendServerMessage($"Cloak {channelName} color updated to {colorIndex}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to update cloak color.", ColorConstants.Red);
        }
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

        if (CurrentEquipmentType == EquipmentType.Weapon && _currentWeapon != null && _currentWeapon.IsValid && backupData.WeaponData != null)
        {
            creature.RunUnequip(_currentWeapon);
            NwItem newWeapon = _currentWeapon.Clone(creature);

            if (newWeapon.IsValid)
            {
                backupData.WeaponData.ApplyToItem(newWeapon);
                creature.RunEquip(newWeapon, InventorySlot.RightHand);
                _currentWeapon.Destroy();
                _currentWeapon = newWeapon;

                WeaponTopModel = backupData.WeaponData.TopModel;
                WeaponMidModel = backupData.WeaponData.MidModel;
                WeaponBotModel = backupData.WeaponData.BotModel;
                WeaponScale = backupData.WeaponData.Scale;

                player.SendServerMessage("Weapon customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                creature.RunEquip(_currentWeapon, InventorySlot.RightHand);
                player.SendServerMessage("Failed to revert weapon changes.", ColorConstants.Red);
            }
        }

        if (CurrentEquipmentType == EquipmentType.Boots && _currentBoots != null && _currentBoots.IsValid && backupData.BootsData != null)
        {
            creature.RunUnequip(_currentBoots);
            NwItem newBoots = _currentBoots.Clone(creature);

            if (newBoots.IsValid)
            {
                backupData.BootsData.ApplyToItem(newBoots);
                creature.RunEquip(newBoots, InventorySlot.Boots);
                _currentBoots.Destroy();
                _currentBoots = newBoots;

                BootsTopModel = backupData.BootsData.TopModel;
                BootsMidModel = backupData.BootsData.MidModel;
                BootsBotModel = backupData.BootsData.BotModel;

                player.SendServerMessage("Boots customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                creature.RunEquip(_currentBoots, InventorySlot.Boots);
                player.SendServerMessage("Failed to revert boots changes.", ColorConstants.Red);
            }
        }

        if (CurrentEquipmentType == EquipmentType.Helmet && _currentHelmet != null && _currentHelmet.IsValid && backupData.HelmetData != null)
        {
            uint copy = NWScript.CopyItemAndModify(_currentHelmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, backupData.HelmetData.Appearance, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NwItem? helmetCopy = copy.ToNwObject<NwItem>();

                for (int i = 0; i < 6; i++)
                {
                    int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                    uint colorCopy = NWScript.CopyItemAndModify(helmetCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, backupData.HelmetData.Colors[i], 1);
                    if (NWScript.GetIsObjectValid(colorCopy) == 1)
                    {
                        if (helmetCopy != null && helmetCopy.IsValid) NWScript.DestroyObject(helmetCopy);
                        helmetCopy = colorCopy.ToNwObject<NwItem>();
                    }
                }

                creature.RunUnequip(_currentHelmet);
                NWScript.DestroyObject(_currentHelmet);
                _currentHelmet = helmetCopy;
                if (_currentHelmet != null) creature.RunEquip(_currentHelmet, InventorySlot.Head);

                HelmetAppearance = backupData.HelmetData.Appearance;

                player.SendServerMessage("Helmet customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                player.SendServerMessage("Failed to revert helmet changes.", ColorConstants.Red);
            }
        }

        if (CurrentEquipmentType == EquipmentType.Cloak && _currentCloak != null && _currentCloak.IsValid && backupData.CloakData != null)
        {
            uint copy = NWScript.CopyItemAndModify(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, backupData.CloakData.Appearance, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NwItem? cloakCopy = copy.ToNwObject<NwItem>();

                for (int i = 0; i < 6; i++)
                {
                    int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                    uint colorCopy = NWScript.CopyItemAndModify(cloakCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, backupData.CloakData.Colors[i], 1);
                    if (NWScript.GetIsObjectValid(colorCopy) == 1)
                    {
                        if (cloakCopy != null && cloakCopy.IsValid) NWScript.DestroyObject(cloakCopy);
                        cloakCopy = colorCopy.ToNwObject<NwItem>();
                    }
                }

                creature.RunUnequip(_currentCloak);
                NWScript.DestroyObject(_currentCloak);
                _currentCloak = cloakCopy;
                if (_currentCloak != null) creature.RunEquip(_currentCloak, InventorySlot.Cloak);

                CloakAppearance = backupData.CloakData.Appearance;

                player.SendServerMessage("Cloak customization reverted to last save point.", ColorConstants.Cyan);
            }
            else
            {
                player.SendServerMessage("Failed to revert cloak changes.", ColorConstants.Red);
            }
        }
    }

    public void ConfirmAndClose()
    {
        ClearBackupFromPcKey();
        player.SendServerMessage("Equipment customization confirmed!", ColorConstants.Green);
    }

    private void SaveBackupToPcKey()
    {
        var backupData = new EquipmentBackupData();

        if (_currentWeapon != null && _currentWeapon.IsValid)
        {
            backupData.WeaponData = WeaponBackupData.FromItem(_currentWeapon);
        }

        if (_currentBoots != null && _currentBoots.IsValid)
        {
            backupData.BootsData = BootsBackupData.FromItem(_currentBoots);
        }

        if (_currentHelmet != null && _currentHelmet.IsValid)
        {
            backupData.HelmetData = HelmetBackupData.FromItem(_currentHelmet);
        }

        if (_currentCloak != null && _currentCloak.IsValid)
        {
            backupData.CloakData = CloakBackupData.FromItem(_currentCloak);
        }

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
    public BootsBackupData? BootsData { get; set; }
    public HelmetBackupData? HelmetData { get; set; }
    public CloakBackupData? CloakData { get; set; }
}

public class WeaponBackupData
{
    public int TopModel { get; set; }
    public int MidModel { get; set; }
    public int BotModel { get; set; }
    public int Scale { get; set; } = 100;

    public static WeaponBackupData FromItem(NwItem weapon)
    {
        VisualTransform transform = weapon.VisualTransform;
        int scale = (int)(transform.Scale * 100);

        return new WeaponBackupData
        {
            TopModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top),
            MidModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle),
            BotModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom),
            Scale = scale
        };
    }

    public void ApplyToItem(NwItem weapon)
    {
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)TopModel);
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)MidModel);
        weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BotModel);

        float scaleValue = Scale / 100f;
        NWScript.SetObjectVisualTransform(weapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
    }
}

public class BootsBackupData
{
    public int TopModel { get; set; }
    public int MidModel { get; set; }
    public int BotModel { get; set; }

    public static BootsBackupData FromItem(NwItem boots)
    {
        return new BootsBackupData
        {
            TopModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top),
            MidModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle),
            BotModel = boots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom)
        };
    }

    public void ApplyToItem(NwItem boots)
    {
        boots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)TopModel);
        boots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)MidModel);
        boots.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BotModel);
    }
}

public class HelmetBackupData
{
    public int Appearance { get; set; }
    public int[] Colors { get; set; } = new int[6];

    public static HelmetBackupData FromItem(NwItem helmet)
    {
        var data = new HelmetBackupData
        {
            Appearance = NWScript.GetItemAppearance(helmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0)
        };

        for (int i = 0; i < 6; i++)
        {
            data.Colors[i] = NWScript.GetItemAppearance(helmet, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i);
        }

        return data;
    }
}

public class CloakBackupData
{
    public int Appearance { get; set; }
    public int[] Colors { get; set; } = new int[6];

    public static CloakBackupData FromItem(NwItem cloak)
    {
        var data = new CloakBackupData
        {
            Appearance = NWScript.GetItemAppearance(cloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0)
        };

        for (int i = 0; i < 6; i++)
        {
            data.Colors[i] = NWScript.GetItemAppearance(cloak, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i);
        }

        return data;
    }
}

