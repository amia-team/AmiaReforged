using Anvil.API;
using Newtonsoft.Json;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ItemTool;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public enum EquipmentType
{
    None,
    Weapon,
    OffHand,
    Boots,
    Helmet,
    Cloak
}

public enum IconAdjustResult { Success, NotAllowedType, NoSelection, NoValidModel }

public sealed class EquipmentCustomizationModel(NwPlayer player)
{
    private const string WeaponBackupKey = "EQUIPMENT_CUSTOMIZATION_WEAPON_BACKUP";
    private const string OffHandBackupKey = "EQUIPMENT_CUSTOMIZATION_OFFHAND_BACKUP";
    private const string BootsBackupKey = "EQUIPMENT_CUSTOMIZATION_BOOTS_BACKUP";
    private const string HelmetBackupKey = "EQUIPMENT_CUSTOMIZATION_HELMET_BACKUP";
    private const string CloakBackupKey = "EQUIPMENT_CUSTOMIZATION_CLOAK_BACKUP";

    public EquipmentType CurrentEquipmentType { get; private set; } = EquipmentType.None;

    private NwItem? _currentWeapon;
    private NwItem? _currentOffHand;
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
    private bool _weaponIsSimple;
    public bool WeaponIsSimple => _weaponIsSimple;

    private int _weaponTopModelMax = 255;
    private int _weaponMidModelMax = 255;
    private int _weaponBotModelMax = 255;

    public int OffHandTopModel { get; private set; } = 1;
    public int OffHandMidModel { get; private set; } = 1;
    public int OffHandBotModel { get; private set; } = 1;
    public int OffHandScale { get; private set; } = 100;

    private bool _offHandOnlyScaleChanged;
    private bool _offHandIsSimple;
    public bool OffHandIsSimple => _offHandIsSimple;

    private int _offHandTopModelMax = 255;
    private int _offHandMidModelMax = 255;
    private int _offHandBotModelMax = 255;

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

        // Reload the current item from inventory (player may have equipped something new)
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        switch (type)
        {
            case EquipmentType.Weapon:
                _currentWeapon = creature.GetItemInSlot(InventorySlot.RightHand);
                if (_currentWeapon != null && _currentWeapon.IsValid)
                {
                    // Check if it's a simple item
                    _weaponIsSimple = _currentWeapon.BaseItem.ModelType == BaseItemModelType.Simple;

                    int modelRange = (int)_currentWeapon.BaseItem.ModelRangeMax;
                    _weaponTopModelMax = modelRange;
                    _weaponMidModelMax = modelRange;
                    _weaponBotModelMax = modelRange;

                    if (_weaponIsSimple)
                    {
                        // For simple items, use simple model appearance
                        WeaponTopModel = NWScript.GetItemAppearance(_currentWeapon, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                        WeaponMidModel = 1;
                        WeaponBotModel = 1;
                    }
                    else
                    {
                        WeaponTopModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
                        WeaponMidModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
                        WeaponBotModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
                    }

                    VisualTransform transform = _currentWeapon.VisualTransform;
                    WeaponScale = (int)(transform.Scale * 100);
                }
                LoadWeaponData();
                break;
            case EquipmentType.OffHand:
                _currentOffHand = creature.GetItemInSlot(InventorySlot.LeftHand);
                if (_currentOffHand != null && _currentOffHand.IsValid)
                {
                    // Check if it's a simple item
                    _offHandIsSimple = _currentOffHand.BaseItem.ModelType == BaseItemModelType.Simple;

                    int modelRange = (int)_currentOffHand.BaseItem.ModelRangeMax;
                    _offHandTopModelMax = modelRange;
                    _offHandMidModelMax = modelRange;
                    _offHandBotModelMax = modelRange;

                    if (_offHandIsSimple)
                    {
                        // For simple items, use simple model appearance
                        OffHandTopModel = NWScript.GetItemAppearance(_currentOffHand, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                        OffHandMidModel = 1;
                        OffHandBotModel = 1;
                    }
                    else
                    {
                        // For complex items, use weapon model parts
                        OffHandTopModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
                        OffHandMidModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
                        OffHandBotModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
                    }

                    VisualTransform transform = _currentOffHand.VisualTransform;
                    OffHandScale = (int)(transform.Scale * 100);
                }
                LoadOffHandData();
                break;
            case EquipmentType.Boots:
                _currentBoots = creature.GetItemInSlot(InventorySlot.Boots);
                if (_currentBoots != null && _currentBoots.IsValid)
                {
                    int modelRange = (int)_currentBoots.BaseItem.ModelRangeMax;
                    _bootsTopModelMax = modelRange;
                    _bootsMidModelMax = modelRange;
                    _bootsBotModelMax = modelRange;

                    BootsTopModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
                    BootsMidModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
                    BootsBotModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
                }
                LoadBootsData();
                break;
            case EquipmentType.Helmet:
                _currentHelmet = creature.GetItemInSlot(InventorySlot.Head);
                if (_currentHelmet != null && _currentHelmet.IsValid)
                {
                    _helmetAppearanceMax = (int)_currentHelmet.BaseItem.ModelRangeMax;
                    HelmetAppearance = NWScript.GetItemAppearance(_currentHelmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                }
                LoadHelmetData();
                break;
            case EquipmentType.Cloak:
                _currentCloak = creature.GetItemInSlot(InventorySlot.Cloak);
                if (_currentCloak != null && _currentCloak.IsValid)
                {
                    _cloakAppearanceMax = 86;
                    CloakAppearance = NWScript.GetItemAppearance(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                }
                LoadCloakData();
                break;
        }
    }

    // Called when window first opens to load ALL equipment and save initial backup
    public void InitializeAllEquipment()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        // Load all equipment references
        _currentWeapon = creature.GetItemInSlot(InventorySlot.RightHand);
        _currentOffHand = creature.GetItemInSlot(InventorySlot.LeftHand);
        _currentBoots = creature.GetItemInSlot(InventorySlot.Boots);
        _currentHelmet = creature.GetItemInSlot(InventorySlot.Head);
        _currentCloak = creature.GetItemInSlot(InventorySlot.Cloak);

        // Load current values for all equipped items
        if (_currentWeapon != null && _currentWeapon.IsValid)
        {
            _weaponIsSimple = _currentWeapon.BaseItem.ModelType == BaseItemModelType.Simple;

            int modelRange = (int)_currentWeapon.BaseItem.ModelRangeMax;
            _weaponTopModelMax = modelRange;
            _weaponMidModelMax = modelRange;
            _weaponBotModelMax = modelRange;

            if (_weaponIsSimple)
            {
                // Simple weapons (shurikens, darts) only use top model
                WeaponTopModel = NWScript.GetItemAppearance(_currentWeapon, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                WeaponMidModel = 0; // Blank out for simple models
                WeaponBotModel = 0; // Blank out for simple models
            }
            else
            {
                // Complex weapons use all three model parts
                WeaponTopModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
                WeaponMidModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
                WeaponBotModel = _currentWeapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
            }

            VisualTransform transform = _currentWeapon.VisualTransform;
            WeaponScale = (int)(transform.Scale * 100);
        }

        if (_currentOffHand != null && _currentOffHand.IsValid)
        {
            _offHandIsSimple = _currentOffHand.BaseItem.ModelType == BaseItemModelType.Simple;

            int modelRange = (int)_currentOffHand.BaseItem.ModelRangeMax;
            _offHandTopModelMax = modelRange;
            _offHandMidModelMax = modelRange;
            _offHandBotModelMax = modelRange;

            if (_offHandIsSimple)
            {
                OffHandTopModel = NWScript.GetItemAppearance(_currentOffHand, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
                OffHandMidModel = 1;
                OffHandBotModel = 1;
            }
            else
            {
                OffHandTopModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
                OffHandMidModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
                OffHandBotModel = _currentOffHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
            }

            VisualTransform transform = _currentOffHand.VisualTransform;
            OffHandScale = (int)(transform.Scale * 100);
        }

        if (_currentBoots != null && _currentBoots.IsValid)
        {
            int modelRange = (int)_currentBoots.BaseItem.ModelRangeMax;
            _bootsTopModelMax = modelRange;
            _bootsMidModelMax = modelRange;
            _bootsBotModelMax = modelRange;

            BootsTopModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            BootsMidModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            BootsBotModel = _currentBoots.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
        }

        if (_currentHelmet != null && _currentHelmet.IsValid)
        {
            _helmetAppearanceMax = (int)_currentHelmet.BaseItem.ModelRangeMax;
            HelmetAppearance = NWScript.GetItemAppearance(_currentHelmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
        }

        if (_currentCloak != null && _currentCloak.IsValid)
        {
            _cloakAppearanceMax = 86;
            CloakAppearance = NWScript.GetItemAppearance(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
        }

        // Save initial backup of ALL equipment separately
        SaveAllBackupsToPcKey();
    }

    private void LoadWeaponData()
    {
        if (_currentWeapon != null && _currentWeapon.IsValid)
        {
            player.SendServerMessage($"Selected main hand item: {_currentWeapon.Name}", ColorConstants.Cyan);
        }
        else
        {
            player.SendServerMessage("Nothing equipped in main hand.", ColorConstants.Orange);
        }
    }

    private void LoadOffHandData()
    {
        if (_currentOffHand != null && _currentOffHand.IsValid)
        {
            string itemType = _offHandIsSimple ? "simple item" : "weapon/shield";
            player.SendServerMessage($"Selected off-hand item ({itemType}): {_currentOffHand.Name}", ColorConstants.Cyan);
        }
        else
        {
            player.SendServerMessage("Nothing equipped in off-hand.", ColorConstants.Orange);
        }
    }

    private void LoadBootsData()
    {
        if (_currentBoots != null && _currentBoots.IsValid)
        {
            player.SendServerMessage($"Selected boots: {_currentBoots.Name}", ColorConstants.Cyan);
        }
        else
        {
            player.SendServerMessage("No boots equipped.", ColorConstants.Orange);
        }
    }

    private void LoadHelmetData()
    {
        if (_currentHelmet != null && _currentHelmet.IsValid)
        {
            player.SendServerMessage($"Selected helmet: {_currentHelmet.Name}", ColorConstants.Cyan);
        }
        else
        {
            player.SendServerMessage("No helmet equipped.", ColorConstants.Orange);
        }
    }

    private void LoadCloakData()
    {
        if (_currentCloak != null && _currentCloak.IsValid)
        {
            player.SendServerMessage($"Selected cloak: {_currentCloak.Name}", ColorConstants.Cyan);
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

        // Use ItemToolModel logic for shurikens and darts
        if (IsShurikenOrDart())
        {
            IconAdjustResult result = TryAdjustIcon(delta, out int newValue, out int maxValue);

            switch (result)
            {
                case IconAdjustResult.Success:
                    WeaponTopModel = newValue;
                    player.SendServerMessage($"Weapon model set to {WeaponTopModel} (max: {maxValue}).", ColorConstants.Green);
                    break;
                case IconAdjustResult.NotAllowedType:
                    player.SendServerMessage("This weapon type does not support model changes.", ColorConstants.Orange);
                    break;
                case IconAdjustResult.NoValidModel:
                    player.SendServerMessage("No other valid models found.", ColorConstants.Orange);
                    break;
                default:
                    player.SendServerMessage("Failed to adjust weapon model.", ColorConstants.Orange);
                    break;
            }
            return;
        }

        // Use standard parts-based logic for other weapons
        int previousModel = WeaponTopModel;
        WeaponTopModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Top, WeaponTopModel, delta, _weaponTopModelMax);

        // Only apply changes if the model actually changed
        if (WeaponTopModel != previousModel)
        {
            ApplyWeaponChanges();
            player.SendServerMessage($"Weapon top model set to {WeaponTopModel}.", ColorConstants.Green);
        }
    }

    public void AdjustWeaponMidModel(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        if (_weaponIsSimple)
        {
            player.SendServerMessage("Middle and bottom models are disabled for simple weapons.", ColorConstants.Orange);
            return;
        }

        _onlyScaleChanged = false;
        int previousModel = WeaponMidModel;
        WeaponMidModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Middle, WeaponMidModel, delta, _weaponMidModelMax);

        // Only apply changes if the model actually changed
        if (WeaponMidModel != previousModel)
        {
            ApplyWeaponChanges();
            player.SendServerMessage($"Weapon middle model set to {WeaponMidModel}.", ColorConstants.Green);
        }
    }

    public void AdjustWeaponBotModel(int delta)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon selected.", ColorConstants.Orange);
            return;
        }

        if (_weaponIsSimple)
        {
            player.SendServerMessage("Middle and bottom models are disabled for simple weapons.", ColorConstants.Orange);
            return;
        }

        _onlyScaleChanged = false;
        int previousModel = WeaponBotModel;
        WeaponBotModel = GetNextValidWeaponModel(ItemAppearanceWeaponModel.Bottom, WeaponBotModel, delta, _weaponBotModelMax);

        // Only apply changes if the model actually changed
        if (WeaponBotModel != previousModel)
        {
            ApplyWeaponChanges();
            player.SendServerMessage($"Weapon bottom model set to {WeaponBotModel}.", ColorConstants.Green);
        }
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

    // Helper methods for shurikens/darts (simple model items in main hand)
    private bool IsShurikenOrDart()
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return false;
        string itemClass = _currentWeapon.BaseItem.ItemClass;
        return itemClass == "WThDt" || itemClass == "WThSh";
    }

    private bool IsIconAllowed(out int current, out int max)
    {
        current = 0; max = 0;
        if (_currentWeapon is null) return false;

        if (!ItemModelValidation.SupportsModelChanges(_currentWeapon))
            return false;

        max = ItemModelValidation.GetMaxModelIndex(_currentWeapon);
        if (max == 0) return false;

        current = NWScript.GetItemAppearance(_currentWeapon, (int)ItemAppearanceType.SimpleModel, 0);
        return true;
    }

    private IconAdjustResult TryAdjustIcon(int delta, out int newValue, out int maxValue)
    {
        newValue = 0; maxValue = 0;
        if (_currentWeapon is null) return IconAdjustResult.NoSelection;

        if (!ItemModelValidation.SupportsModelChanges(_currentWeapon))
            return IconAdjustResult.NotAllowedType;

        maxValue = ItemModelValidation.GetMaxModelIndex(_currentWeapon);
        if (maxValue == 0)
            return IconAdjustResult.NotAllowedType;

        int current = NWScript.GetItemAppearance(_currentWeapon, (int)ItemAppearanceType.SimpleModel, 0);
        if (delta == 0)
        {
            newValue = current;
            return IconAdjustResult.Success;
        }

        int candidate = GetNextValidItemModel(current, delta, maxValue);

        if (candidate == current)
        {
            return IconAdjustResult.NoValidModel;
        }

        NwCreature? creature = player.ControlledCreature;
        InventorySlot? equippedSlot = null;

        if (creature != null)
        {
            foreach (InventorySlot slot in Enum.GetValues<InventorySlot>())
            {
                if (creature.GetItemInSlot(slot) == _currentWeapon)
                {
                    equippedSlot = slot;
                    break;
                }
            }

            if (equippedSlot.HasValue)
            {
                creature.RunUnequip(_currentWeapon);
            }
        }

        uint copy = NWScript.CopyItemAndModify(_currentWeapon, (int)ItemAppearanceType.SimpleModel, 0, candidate, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NWScript.DestroyObject(_currentWeapon);
            _currentWeapon = copy.ToNwObject<NwItem>();

            if (creature != null && equippedSlot.HasValue && _currentWeapon != null)
            {
                creature.RunEquip(_currentWeapon, equippedSlot.Value);
            }

            newValue = candidate;
            return IconAdjustResult.Success;
        }

        if (creature != null && equippedSlot.HasValue && _currentWeapon != null)
        {
            creature.RunEquip(_currentWeapon, equippedSlot.Value);
        }

        newValue = current;
        return IconAdjustResult.NoValidModel;
    }

    private int GetNextValidItemModel(int currentModel, int delta, int maxModel)
    {
        if (_currentWeapon == null || !_currentWeapon.IsValid) return currentModel;
        if (maxModel <= 0) return currentModel;

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
                return currentModel;
            }

            if (ItemModelValidation.IsValidModelIndex(_currentWeapon, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        return currentModel;
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

        // Check if weapon uses simple or complex model type
        // Check if base item type is stackable (from baseitems.2da "Stacking" column)
        int maxStackSize = NWScript.StringToInt(NWScript.Get2DAString("baseitems", "Stacking", (int)currentWeapon.BaseItem.ItemType));
        bool isBaseItemStackable = maxStackSize > 1;
        bool isSimpleModel = currentWeapon.BaseItem.ModelType == BaseItemModelType.Simple;

        if (isSimpleModel && isBaseItemStackable)
        {
            // Simple model stackable - use CopyItemAndModify which preserves stack size automatically
            player.SendServerMessage($"[DEBUG] Simple stackable: Using EXACT ItemTool pattern", ColorConstants.Yellow);

            // Unequip if equipped
            creature.RunUnequip(currentWeapon);

            // EXACT ItemTool: CopyItemAndModify directly
            uint copy = NWScript.CopyItemAndModify(currentWeapon, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, WeaponTopModel, 1);
            player.SendServerMessage($"[DEBUG] CopyItemAndModify result: {copy}, valid: {NWScript.GetIsObjectValid(copy)}", ColorConstants.Yellow);

            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                // EXACT ItemTool: Destroy using NWScript.DestroyObject on the ORIGINAL
                player.SendServerMessage($"[DEBUG] Destroying original ObjectId: {currentWeapon.ObjectId}", ColorConstants.Yellow);
                NWScript.DestroyObject(currentWeapon);

                // EXACT ItemTool: Update reference to the copy IMMEDIATELY
                _currentWeapon = copy.ToNwObject<NwItem>();

                if (_currentWeapon != null && _currentWeapon.IsValid)
                {
                    player.SendServerMessage($"[DEBUG] Updated reference. New ObjectId: {_currentWeapon.ObjectId}, Stack: {_currentWeapon.StackSize}", ColorConstants.Yellow);

                    // Apply scale
                    NWScript.SetObjectVisualTransform(_currentWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                    // Re-equip
                    creature.RunEquip(_currentWeapon, InventorySlot.RightHand);

                    player.SendServerMessage($"[DEBUG] ItemTool pattern complete! Final stack: {_currentWeapon.StackSize}", ColorConstants.Green);
                    return;
                }
                else
                {
                    player.SendServerMessage("Failed to convert copy to NwItem.", ColorConstants.Red);
                    return;
                }
            }
            else
            {
                player.SendServerMessage("Failed to create modified copy.", ColorConstants.Red);
                creature.RunEquip(currentWeapon, InventorySlot.RightHand);
                return;
            }
        }
        else if (isBaseItemStackable)
        {
            // Complex stackable weapons (e.g., throwing axes, bouquets)
            // CopyItemAndModify doesn't work for stackable WEAPON_MODEL types
            // Workaround: Create copy at remote waypoint to prevent stack merging, then move to player
            player.SendServerMessage($"[DEBUG] Using waypoint workaround for composite stackable weapon", ColorConstants.Yellow);

            // Store original stack size
            int originalStackSize = currentWeapon.StackSize;

            // Find the ds_copy waypoint in core_atr area
            NwArea? targetArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == "core_atr");
            if (targetArea == null)
            {
                player.SendServerMessage("Failed to find copy area.", ColorConstants.Red);
                return;
            }

            NwWaypoint? copyWaypoint = targetArea.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");
            if (copyWaypoint == null)
            {
                player.SendServerMessage("Failed to find copy waypoint.", ColorConstants.Red);
                return;
            }

            player.SendServerMessage($"[DEBUG] Found waypoint at {copyWaypoint.Area.Name}", ColorConstants.Yellow);

            // Unequip the weapon
            creature.RunUnequip(currentWeapon);

            // Create copy at the waypoint (prevents stack merging with original)
            uint copyId = NWScript.CopyItem(currentWeapon, copyWaypoint, 1);
            player.SendServerMessage($"[DEBUG] CopyItem to waypoint result: {copyId}, valid: {NWScript.GetIsObjectValid(copyId)}", ColorConstants.Yellow);

            if (NWScript.GetIsObjectValid(copyId) == 0)
            {
                player.SendServerMessage("Failed to copy weapon.", ColorConstants.Red);
                creature.RunEquip(currentWeapon, InventorySlot.RightHand);
                return;
            }

            NwItem? copiedWeapon = copyId.ToNwObject<NwItem>();
            if (copiedWeapon == null || !copiedWeapon.IsValid)
            {
                NWScript.DestroyObject(copyId);
                player.SendServerMessage("Failed to convert weapon.", ColorConstants.Red);
                creature.RunEquip(currentWeapon, InventorySlot.RightHand);
                return;
            }

            // Manually set the stack size on the copy
            copiedWeapon.StackSize = originalStackSize;
            player.SendServerMessage($"[DEBUG] Set stack size to {originalStackSize}, actual: {copiedWeapon.StackSize}", ColorConstants.Yellow);

            // Manually modify the weapon appearance
            copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)WeaponTopModel);
            copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)WeaponMidModel);
            copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)WeaponBotModel);

            // Apply scale
            NWScript.SetObjectVisualTransform(copiedWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

            // NOW destroy the original (copy is safe at waypoint)
            currentWeapon.Destroy();
            player.SendServerMessage($"[DEBUG] Original destroyed", ColorConstants.Yellow);

            // Move the copy from waypoint to player
            NwItem? newWeapon = copiedWeapon.Clone(creature);
            if (newWeapon == null || !newWeapon.IsValid)
            {
                player.SendServerMessage("Failed to move weapon to player.", ColorConstants.Red);
                return;
            }

            // Destroy the waypoint copy
            copiedWeapon.Destroy();
            player.SendServerMessage($"[DEBUG] Moved copy to player, waypoint copy destroyed", ColorConstants.Yellow);

            // Equip the new weapon
            creature.RunEquip(newWeapon, InventorySlot.RightHand);
            _currentWeapon = newWeapon;

            player.SendServerMessage($"[DEBUG] Weapon changes applied successfully! Final stack: {newWeapon.StackSize}", ColorConstants.Green);
        }
        else
        {
            // For non-stackable items, use the normal clone/destroy method
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
    }

    // Off-Hand Methods
    public void AdjustOffHandTopModel(int delta)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid)
        {
            player.SendServerMessage("No off-hand item selected.", ColorConstants.Orange);
            return;
        }

        _offHandOnlyScaleChanged = false;

        if (_offHandIsSimple)
        {
            OffHandTopModel = GetNextValidOffHandSimpleModel(OffHandTopModel, delta, _offHandTopModelMax);
            ApplyOffHandChanges();
            player.SendServerMessage($"Off-hand model set to {OffHandTopModel}.", ColorConstants.Green);
        }
        else
        {
            OffHandTopModel = GetNextValidOffHandWeaponModel(ItemAppearanceWeaponModel.Top, OffHandTopModel, delta, _offHandTopModelMax);
            ApplyOffHandChanges();
            player.SendServerMessage($"Off-hand top model set to {OffHandTopModel}.", ColorConstants.Green);
        }
    }

    public void AdjustOffHandMidModel(int delta)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid)
        {
            player.SendServerMessage("No off-hand item selected.", ColorConstants.Orange);
            return;
        }

        if (_offHandIsSimple)
        {
            player.SendServerMessage("Middle and bottom models are disabled for simple items.", ColorConstants.Orange);
            return;
        }

        _offHandOnlyScaleChanged = false;
        OffHandMidModel = GetNextValidOffHandWeaponModel(ItemAppearanceWeaponModel.Middle, OffHandMidModel, delta, _offHandMidModelMax);
        ApplyOffHandChanges();
        player.SendServerMessage($"Off-hand middle model set to {OffHandMidModel}.", ColorConstants.Green);
    }

    public void AdjustOffHandBotModel(int delta)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid)
        {
            player.SendServerMessage("No off-hand item selected.", ColorConstants.Orange);
            return;
        }

        if (_offHandIsSimple)
        {
            player.SendServerMessage("Middle and bottom models are disabled for simple items.", ColorConstants.Orange);
            return;
        }

        _offHandOnlyScaleChanged = false;
        int previousModel = OffHandBotModel;
        OffHandBotModel = GetNextValidOffHandWeaponModel(ItemAppearanceWeaponModel.Bottom, OffHandBotModel, delta, _offHandBotModelMax);

        // Only apply changes if the model actually changed
        if (OffHandBotModel != previousModel)
        {
            ApplyOffHandChanges();
            player.SendServerMessage($"Off-hand bottom model set to {OffHandBotModel}.", ColorConstants.Green);
        }
    }

    public void AdjustOffHandScale(int delta)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid)
        {
            player.SendServerMessage("No off-hand item selected.", ColorConstants.Orange);
            return;
        }

        int newScale = OffHandScale + delta;

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

        _offHandOnlyScaleChanged = true;
        OffHandScale = newScale;
        ApplyOffHandChanges();

        int percentDiff = OffHandScale - 100;
        string sign = percentDiff > 0 ? "+" : "";
        player.SendServerMessage($"Off-hand scale set to {OffHandScale}% ({sign}{percentDiff}%).", ColorConstants.Green);
    }

    private int GetNextValidOffHandSimpleModel(int currentModel, int delta, int maxModel)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid) return currentModel;
        if (maxModel <= 0) return currentModel;

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

            if (IsValidOffHandSimpleModel(searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid off-hand model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidOffHandSimpleModel(int modelIndex)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid) return false;
        if (modelIndex < 0) return false;

        string itemClass = _currentOffHand.BaseItem.ItemClass;
        if (string.IsNullOrEmpty(itemClass)) return false;

        uint baseItemId = _currentOffHand.BaseItem.Id;
        bool usesMdlWithoutPrefix = _currentOffHand.BaseItem.ItemType == BaseItemType.SmallShield
                                     || _currentOffHand.BaseItem.ItemType == BaseItemType.LargeShield
                                     || _currentOffHand.BaseItem.ItemType == BaseItemType.TowerShield
                                     || baseItemId == 213
                                     || baseItemId == 214
                                     || baseItemId == 215;

        string modelResRef;
        int resType;

        if (usesMdlWithoutPrefix)
        {
            modelResRef = $"{itemClass}_{modelIndex:D3}";
            resType = NWScript.RESTYPE_MDL;
        }
        else
        {
            modelResRef = $"i{itemClass}_{modelIndex:D3}";
            resType = NWScript.RESTYPE_TGA;
        }

        string alias = NWScript.ResManGetAliasFor(modelResRef, resType);
        return !string.IsNullOrEmpty(alias);
    }

    private int GetNextValidOffHandWeaponModel(ItemAppearanceWeaponModel modelPart, int currentModel, int delta, int maxModel)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid) return currentModel;
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

            if (IsValidOffHandWeaponModel(modelPart, searchModel))
            {
                return searchModel;
            }

            attemptsRemaining--;
        }

        player.SendServerMessage("Could not find a valid off-hand model.", ColorConstants.Orange);
        return currentModel;
    }

    private bool IsValidOffHandWeaponModel(ItemAppearanceWeaponModel modelPart, int modelNumber)
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid) return false;
        if (modelNumber == 0) return false;

        string itemClass = _currentOffHand.BaseItem.ItemClass;
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

    private void ApplyOffHandChanges()
    {
        if (_currentOffHand == null || !_currentOffHand.IsValid) return;

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        NwItem currentOffHand = _currentOffHand;
        float scaleValue = OffHandScale / 100f;
        NWScript.SetObjectVisualTransform(currentOffHand, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

        if (_offHandOnlyScaleChanged)
        {
            return;
        }

        if (_offHandIsSimple)
        {
            // For simple items, use CopyItemAndModify with simple model
            // Note: CopyItemAndModify preserves stack size automatically
            creature.RunUnequip(currentOffHand);
            uint copy = NWScript.CopyItemAndModify(currentOffHand, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, OffHandTopModel, 1);

            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NWScript.DestroyObject(currentOffHand);
                _currentOffHand = copy.ToNwObject<NwItem>();

                if (_currentOffHand != null && _currentOffHand.IsValid)
                {
                    creature.RunEquip(_currentOffHand, InventorySlot.LeftHand);
                    NWScript.SetObjectVisualTransform(_currentOffHand, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
                }
            }
            else
            {
                player.SendServerMessage("Failed to refresh off-hand item.", ColorConstants.Red);
                creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
            }
        }
        else
        {
            // For complex items (weapons)
            bool isStackable = currentOffHand.StackSize > 1;
            bool isSimpleModel = currentOffHand.BaseItem.ModelType == BaseItemModelType.Simple;

            if (isStackable && !isSimpleModel)
            {
                // Composite/Complex stackable off-hand - use waypoint workaround
                player.SendServerMessage($"[DEBUG] Using waypoint workaround for composite stackable off-hand", ColorConstants.Yellow);

                int originalStackSize = currentOffHand.StackSize;

                // Find the ds_copy waypoint in core_atr area
                NwArea? targetArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == "core_atr");
                if (targetArea == null)
                {
                    player.SendServerMessage("Failed to find copy area.", ColorConstants.Red);
                    return;
                }

                NwWaypoint? copyWaypoint = targetArea.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");
                if (copyWaypoint == null)
                {
                    player.SendServerMessage("Failed to find copy waypoint.", ColorConstants.Red);
                    return;
                }

                // Unequip the weapon
                creature.RunUnequip(currentOffHand);

                // Create copy at the waypoint (prevents stack merging with original)
                uint copyId = NWScript.CopyItem(currentOffHand, copyWaypoint, 1);

                if (NWScript.GetIsObjectValid(copyId) == 0)
                {
                    player.SendServerMessage("Failed to copy off-hand weapon.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                NwItem? copiedWeapon = copyId.ToNwObject<NwItem>();
                if (copiedWeapon == null || !copiedWeapon.IsValid)
                {
                    NWScript.DestroyObject(copyId);
                    player.SendServerMessage("Failed to convert off-hand weapon.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                // Manually set the stack size on the copy
                copiedWeapon.StackSize = originalStackSize;

                // Manually modify the weapon appearance
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)OffHandTopModel);
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)OffHandMidModel);
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)OffHandBotModel);

                // Apply scale
                NWScript.SetObjectVisualTransform(copiedWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                // NOW destroy the original (copy is safe at waypoint)
                currentOffHand.Destroy();

                // Move the copy from waypoint to player
                NwItem? newOffHand = copiedWeapon.Clone(creature);
                if (newOffHand == null || !newOffHand.IsValid)
                {
                    player.SendServerMessage("Failed to move off-hand weapon to player.", ColorConstants.Red);
                    return;
                }

                // Destroy the waypoint copy
                copiedWeapon.Destroy();

                // Equip the new weapon
                creature.RunEquip(newOffHand, InventorySlot.LeftHand);
                _currentOffHand = newOffHand;

                player.SendServerMessage($"[DEBUG] Off-hand weapon changes applied successfully! Final stack: {newOffHand.StackSize}", ColorConstants.Green);
            }
            else if (isStackable && isSimpleModel)
            {
                // Simple stackable off-hand - MUST use waypoint workaround
                player.SendServerMessage($"[DEBUG] Simple stackable off-hand: Using waypoint workaround", ColorConstants.Yellow);

                int originalStackSize = currentOffHand.StackSize;

                // Find waypoint
                NwArea? targetArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == "core_atr");
                if (targetArea == null)
                {
                    player.SendServerMessage("Failed to find copy area.", ColorConstants.Red);
                    return;
                }

                NwWaypoint? copyWaypoint = targetArea.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");
                if (copyWaypoint == null)
                {
                    player.SendServerMessage("Failed to find copy waypoint.", ColorConstants.Red);
                    return;
                }

                // Unequip
                creature.RunUnequip(currentOffHand);

                // Copy to waypoint
                uint copyId = NWScript.CopyItem(currentOffHand, copyWaypoint, 1);

                if (NWScript.GetIsObjectValid(copyId) == 0)
                {
                    player.SendServerMessage("Failed to copy off-hand.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                NwItem? waypointCopy = copyId.ToNwObject<NwItem>();
                if (waypointCopy == null || !waypointCopy.IsValid)
                {
                    NWScript.DestroyObject(copyId);
                    player.SendServerMessage("Failed to convert off-hand.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                // Set stack size
                waypointCopy.StackSize = originalStackSize;

                // Modify appearance at waypoint
                uint modifiedId = NWScript.CopyItemAndModify(waypointCopy, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, OffHandTopModel, 1);

                // Destroy unmodified copy
                waypointCopy.Destroy();

                if (NWScript.GetIsObjectValid(modifiedId) == 0)
                {
                    player.SendServerMessage("Failed to modify off-hand appearance.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                NwItem? modifiedCopy = modifiedId.ToNwObject<NwItem>();
                if (modifiedCopy == null || !modifiedCopy.IsValid)
                {
                    NWScript.DestroyObject(modifiedId);
                    player.SendServerMessage("Failed to convert modified off-hand.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                // Ensure stack size
                modifiedCopy.StackSize = originalStackSize;

                // Apply scale
                NWScript.SetObjectVisualTransform(modifiedCopy, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                // Destroy original
                currentOffHand.Destroy();

                // Clone to player
                NwItem? newOffHand = modifiedCopy.Clone(creature);
                if (newOffHand == null || !newOffHand.IsValid)
                {
                    player.SendServerMessage("Failed to move off-hand to player.", ColorConstants.Red);
                    return;
                }

                // Destroy waypoint copy
                modifiedCopy.Destroy();

                // Equip
                creature.RunEquip(newOffHand, InventorySlot.LeftHand);
                _currentOffHand = newOffHand;

                player.SendServerMessage($"[DEBUG] Off-hand waypoint workaround complete! Final stack: {newOffHand.StackSize}", ColorConstants.Green);
            }
            else
            {
                // For non-stackable items, use the normal clone/destroy method
                currentOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)OffHandTopModel);
                currentOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)OffHandMidModel);
                currentOffHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)OffHandBotModel);

                creature.RunUnequip(currentOffHand);
                NwItem newOffHand = currentOffHand.Clone(creature);

                if (!newOffHand.IsValid)
                {
                    player.SendServerMessage("Failed to refresh off-hand item.", ColorConstants.Red);
                    creature.RunEquip(currentOffHand, InventorySlot.LeftHand);
                    return;
                }

                creature.RunEquip(newOffHand, InventorySlot.LeftHand);
                NWScript.SetObjectVisualTransform(newOffHand, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
                _currentOffHand = newOffHand;
                currentOffHand.Destroy();
            }
        }
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

        int previousAppearance = HelmetAppearance;
        HelmetAppearance = GetNextValidHelmetAppearance(HelmetAppearance, delta, _helmetAppearanceMax);

        // Only apply changes if the appearance actually changed
        if (HelmetAppearance != previousAppearance)
        {
            ApplyHelmetChanges();
            player.SendServerMessage($"Helmet appearance set to {HelmetAppearance}.", ColorConstants.Green);
        }
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

        int previousAppearance = CloakAppearance;
        CloakAppearance = GetNextValidCloakAppearance(CloakAppearance, delta, _cloakAppearanceMax);

        // Only apply changes if the appearance actually changed
        if (CloakAppearance != previousAppearance)
        {
            ApplyCloakChanges();
            player.SendServerMessage($"Cloak appearance set to {CloakAppearance}.", ColorConstants.Green);
        }
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
        // Force save the current state as the new backup point for ALL equipment
        SaveAllBackupsToPcKey();
        player.SendServerMessage(
            "Equipment customization saved! You can continue editing or click Revert to return to this save point.",
            ColorConstants.Green);
    }

    public void RevertChanges()
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        bool anyReverted = false;

        // Reload current items from inventory in case player swapped items
        _currentWeapon = creature.GetItemInSlot(InventorySlot.RightHand);
        _currentOffHand = creature.GetItemInSlot(InventorySlot.LeftHand);
        _currentBoots = creature.GetItemInSlot(InventorySlot.Boots);
        _currentHelmet = creature.GetItemInSlot(InventorySlot.Head);
        _currentCloak = creature.GetItemInSlot(InventorySlot.Cloak);

        // Revert weapon if backup exists and item is equipped
        WeaponBackupData? weaponBackup = LoadWeaponBackupFromPcKey();
        if (_currentWeapon != null && _currentWeapon.IsValid && weaponBackup != null)
        {
            // Get current weapon state
            WeaponBackupData currentState = WeaponBackupData.FromItem(_currentWeapon);

            // Only revert if the backup is different from current state
            bool needsRevert = currentState.TopModel != weaponBackup.TopModel ||
                              currentState.MidModel != weaponBackup.MidModel ||
                              currentState.BotModel != weaponBackup.BotModel ||
                              currentState.Scale != weaponBackup.Scale;

            if (!needsRevert)
            {
                // Already at backup state, skip revert
                goto SkipWeaponRevert;
            }

            // Check if base item type is stackable (from baseitems.2da "Stacking" column)
            int maxStackSize = NWScript.StringToInt(NWScript.Get2DAString("baseitems", "Stacking", (int)_currentWeapon.BaseItem.ItemType));
            bool isBaseItemStackable = maxStackSize > 1;

            if (weaponBackup.IsSimpleModel)
            {
                // For simple model weapons - CopyItemAndModify preserves stack size automatically
                uint copy = NWScript.CopyItemAndModify(_currentWeapon, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, weaponBackup.TopModel, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NwItem? weaponCopy = copy.ToNwObject<NwItem>();
                    creature.RunUnequip(_currentWeapon);
                    NWScript.DestroyObject(_currentWeapon);
                    _currentWeapon = weaponCopy;
                    if (_currentWeapon != null)
                    {
                        creature.RunEquip(_currentWeapon, InventorySlot.RightHand);
                        weaponBackup.ApplyToItem(_currentWeapon);
                    }

                    WeaponTopModel = weaponBackup.TopModel;
                    WeaponMidModel = weaponBackup.MidModel;
                    WeaponBotModel = weaponBackup.BotModel;
                    WeaponScale = weaponBackup.Scale;

                    anyReverted = true;
                }
            }
            else if (isBaseItemStackable)
            {
                // For complex stackable weapons (e.g., throwing axes, bouquets)
                // CopyItemAndModify doesn't work - use waypoint workaround

                // Store original stack size
                int originalStackSize = _currentWeapon.StackSize;

                // Find the ds_copy waypoint in core_atr area
                NwArea? targetArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == "core_atr");
                if (targetArea == null)
                {
                    player.SendServerMessage("Failed to revert: copy area not found.", ColorConstants.Red);
                }
                else
                {
                    NwWaypoint? copyWaypoint = targetArea.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");
                    if (copyWaypoint == null)
                    {
                        player.SendServerMessage("Failed to revert: copy waypoint not found.", ColorConstants.Red);
                    }
                    else
                    {
                        // Unequip first
                        creature.RunUnequip(_currentWeapon);

                        // Create copy at waypoint (prevents stack merging)
                        uint copyId = NWScript.CopyItem(_currentWeapon, copyWaypoint, 1);

                        if (NWScript.GetIsObjectValid(copyId) == 1)
                        {
                            NwItem? waypointCopy = copyId.ToNwObject<NwItem>();
                            if (waypointCopy != null && waypointCopy.IsValid)
                            {
                                // Set stack size on waypoint copy
                                waypointCopy.StackSize = originalStackSize;

                                // Apply the backup appearance
                                waypointCopy.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)weaponBackup.TopModel);
                                waypointCopy.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)weaponBackup.MidModel);
                                waypointCopy.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)weaponBackup.BotModel);

                                // Apply scale
                                float scaleValue = weaponBackup.Scale / 100f;
                                NWScript.SetObjectVisualTransform(waypointCopy, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                                // Destroy original weapon
                                _currentWeapon.Destroy();

                                // Copy from waypoint directly to player (don't use Clone as it can merge stacks)
                                uint newWeaponId = NWScript.CopyItem(waypointCopy, creature, 1);

                                // Destroy waypoint copy
                                waypointCopy.Destroy();

                                if (NWScript.GetIsObjectValid(newWeaponId) == 1)
                                {
                                    NwItem? newWeapon = newWeaponId.ToNwObject<NwItem>();
                                    if (newWeapon != null && newWeapon.IsValid)
                                    {
                                        // Equip the new weapon
                                        creature.RunEquip(newWeapon, InventorySlot.RightHand);
                                        _currentWeapon = newWeapon;

                                        WeaponTopModel = weaponBackup.TopModel;
                                        WeaponMidModel = weaponBackup.MidModel;
                                        WeaponBotModel = weaponBackup.BotModel;
                                        WeaponScale = weaponBackup.Scale;

                                        anyReverted = true;
                                    }
                                    else
                                    {
                                        player.SendServerMessage("Failed to convert copied weapon. Original item was destroyed.", ColorConstants.Red);
                                        _currentWeapon = null;
                                    }
                                }
                                else
                                {
                                    player.SendServerMessage("Failed to copy weapon from waypoint. Original item was destroyed.", ColorConstants.Red);
                                    _currentWeapon = null;
                                }
                            }
                            else
                            {
                                NWScript.DestroyObject(copyId);
                                player.SendServerMessage("Failed to convert waypoint copy. Original item was destroyed.", ColorConstants.Red);
                                _currentWeapon = null;
                            }
                        }
                        else
                        {
                            player.SendServerMessage("Failed to copy weapon to waypoint. Original item was destroyed.", ColorConstants.Red);
                            _currentWeapon = null;
                        }
                    }
                }
            }
            else
            {
                // For non-stackable items, use clone/destroy
                creature.RunUnequip(_currentWeapon);
                NwItem newWeapon = _currentWeapon.Clone(creature);

                if (newWeapon.IsValid)
                {
                    weaponBackup.ApplyToItem(newWeapon);
                    creature.RunEquip(newWeapon, InventorySlot.RightHand);
                    _currentWeapon.Destroy();
                    _currentWeapon = newWeapon;

                    WeaponTopModel = weaponBackup.TopModel;
                    WeaponMidModel = weaponBackup.MidModel;
                    WeaponBotModel = weaponBackup.BotModel;
                    WeaponScale = weaponBackup.Scale;

                    anyReverted = true;
                }
                else
                {
                    creature.RunEquip(_currentWeapon, InventorySlot.RightHand);
                }
            }
        }

        SkipWeaponRevert:

        // Revert off-hand if backup exists and item is equipped
        OffHandBackupData? offHandBackup = LoadOffHandBackupFromPcKey();
        if (_currentOffHand != null && _currentOffHand.IsValid && offHandBackup != null)
        {
            // Get current off-hand state
            OffHandBackupData currentState = OffHandBackupData.FromItem(_currentOffHand);

            // Only revert if the backup is different from current state
            bool needsRevert = currentState.TopModel != offHandBackup.TopModel ||
                              currentState.MidModel != offHandBackup.MidModel ||
                              currentState.BotModel != offHandBackup.BotModel ||
                              currentState.Scale != offHandBackup.Scale;

            if (!needsRevert)
            {
                // Already at backup state, skip revert
                goto SkipOffHandRevert;
            }

            if (offHandBackup.IsSimple)
            {
                // For simple items - CopyItemAndModify preserves stack size automatically
                uint copy = NWScript.CopyItemAndModify(_currentOffHand, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, offHandBackup.TopModel, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NwItem? offHandCopy = copy.ToNwObject<NwItem>();
                    creature.RunUnequip(_currentOffHand);
                    NWScript.DestroyObject(_currentOffHand);
                    _currentOffHand = offHandCopy;
                    if (_currentOffHand != null)
                    {
                        creature.RunEquip(_currentOffHand, InventorySlot.LeftHand);
                        offHandBackup.ApplyToItem(_currentOffHand);
                    }

                    OffHandTopModel = offHandBackup.TopModel;
                    OffHandMidModel = offHandBackup.MidModel;
                    OffHandBotModel = offHandBackup.BotModel;
                    OffHandScale = offHandBackup.Scale;

                    anyReverted = true;
                }
            }
            else
            {
                // For complex items (weapons/shields)
                bool isStackable = _currentOffHand.StackSize > 1;

                if (isStackable)
                {
                    // Use CopyItemAndModify for stackable items - call while equipped!
                    uint copy1 = NWScript.CopyItemAndModify(_currentOffHand, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_TOP, offHandBackup.TopModel, 1);
                    if (NWScript.GetIsObjectValid(copy1) == 1)
                    {
                        uint copy2 = NWScript.CopyItemAndModify(copy1, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_MIDDLE, offHandBackup.MidModel, 1);

                        if (NWScript.GetIsObjectValid(copy2) == 1)
                        {
                            uint copy3 = NWScript.CopyItemAndModify(copy2, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_BOTTOM, offHandBackup.BotModel, 1);

                            if (NWScript.GetIsObjectValid(copy3) == 1)
                            {
                                NwItem? newOffHand = copy3.ToNwObject<NwItem>();
                                if (newOffHand != null && newOffHand.IsValid)
                                {
                                    float scaleValue = offHandBackup.Scale / 100f;
                                    NWScript.SetObjectVisualTransform(newOffHand, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                                    // Unequip and destroy original
                                    creature.RunUnequip(_currentOffHand);
                                    _currentOffHand.Destroy();

                                    // Cleanup intermediate copies
                                    NWScript.DestroyObject(copy1);
                                    NWScript.DestroyObject(copy2);

                                    creature.RunEquip(newOffHand, InventorySlot.LeftHand);
                                    _currentOffHand = newOffHand;

                                    OffHandTopModel = offHandBackup.TopModel;
                                    OffHandMidModel = offHandBackup.MidModel;
                                    OffHandBotModel = offHandBackup.BotModel;
                                    OffHandScale = offHandBackup.Scale;

                                    anyReverted = true;
                                }
                                else
                                {
                                    NWScript.DestroyObject(copy1);
                                    NWScript.DestroyObject(copy2);
                                    NWScript.DestroyObject(copy3);
                                }
                            }
                            else
                            {
                                NWScript.DestroyObject(copy1);
                                NWScript.DestroyObject(copy2);
                            }
                        }
                        else
                        {
                            NWScript.DestroyObject(copy1);
                        }
                    }
                }
                else
                {
                    // For non-stackable items, use clone/destroy
                    creature.RunUnequip(_currentOffHand);
                    NwItem newOffHand = _currentOffHand.Clone(creature);

                    if (newOffHand.IsValid)
                    {
                        offHandBackup.ApplyToItem(newOffHand);
                        creature.RunEquip(newOffHand, InventorySlot.LeftHand);
                        _currentOffHand.Destroy();
                        _currentOffHand = newOffHand;

                        OffHandTopModel = offHandBackup.TopModel;
                        OffHandMidModel = offHandBackup.MidModel;
                        OffHandBotModel = offHandBackup.BotModel;
                        OffHandScale = offHandBackup.Scale;

                        anyReverted = true;
                    }
                    else
                    {
                        creature.RunEquip(_currentOffHand, InventorySlot.LeftHand);
                    }
                }
            }
        }

        SkipOffHandRevert:

        // Revert boots if backup exists and item is equipped
        BootsBackupData? bootsBackup = LoadBootsBackupFromPcKey();
        if (_currentBoots != null && _currentBoots.IsValid && bootsBackup != null)
        {
            // Get current boots state
            BootsBackupData currentState = BootsBackupData.FromItem(_currentBoots);

            // Only revert if the backup is different from current state
            bool needsRevert = currentState.TopModel != bootsBackup.TopModel ||
                              currentState.MidModel != bootsBackup.MidModel ||
                              currentState.BotModel != bootsBackup.BotModel;

            if (!needsRevert)
            {
                // Already at backup state, skip revert
                goto SkipBootsRevert;
            }

            creature.RunUnequip(_currentBoots);
            NwItem newBoots = _currentBoots.Clone(creature);

            if (newBoots.IsValid)
            {
                bootsBackup.ApplyToItem(newBoots);
                creature.RunEquip(newBoots, InventorySlot.Boots);
                _currentBoots.Destroy();
                _currentBoots = newBoots;

                BootsTopModel = bootsBackup.TopModel;
                BootsMidModel = bootsBackup.MidModel;
                BootsBotModel = bootsBackup.BotModel;

                anyReverted = true;
            }
            else
            {
                creature.RunEquip(_currentBoots, InventorySlot.Boots);
            }
        }

        SkipBootsRevert:

        // Revert helmet if backup exists and item is equipped
        HelmetBackupData? helmetBackup = LoadHelmetBackupFromPcKey();
        if (_currentHelmet != null && _currentHelmet.IsValid && helmetBackup != null)
        {
            // Get current helmet state
            HelmetBackupData currentState = HelmetBackupData.FromItem(_currentHelmet);

            // Only revert if the backup is different from current state
            if (currentState.Appearance == helmetBackup.Appearance)
            {
                // Already at backup state, skip revert
                goto SkipHelmetRevert;
            }

            uint copy = NWScript.CopyItemAndModify(_currentHelmet, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, helmetBackup.Appearance, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NwItem? helmetCopy = copy.ToNwObject<NwItem>();

                for (int i = 0; i < 6; i++)
                {
                    int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                    uint colorCopy = NWScript.CopyItemAndModify(helmetCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, helmetBackup.Colors[i], 1);
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

                HelmetAppearance = helmetBackup.Appearance;

                anyReverted = true;
            }
        }

        SkipHelmetRevert:

        // Revert cloak if backup exists and item is equipped
        CloakBackupData? cloakBackup = LoadCloakBackupFromPcKey();
        if (_currentCloak != null && _currentCloak.IsValid && cloakBackup != null)
        {
            // Get current cloak state
            CloakBackupData currentState = CloakBackupData.FromItem(_currentCloak);

            // Only revert if the backup is different from current state
            if (currentState.Appearance == cloakBackup.Appearance)
            {
                // Already at backup state, skip revert
                goto SkipCloakRevert;
            }

            uint copy = NWScript.CopyItemAndModify(_currentCloak, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, cloakBackup.Appearance, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NwItem? cloakCopy = copy.ToNwObject<NwItem>();

                for (int i = 0; i < 6; i++)
                {
                    int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                    uint colorCopy = NWScript.CopyItemAndModify(cloakCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, cloakBackup.Colors[i], 1);
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

                CloakAppearance = cloakBackup.Appearance;

                anyReverted = true;
            }
        }

        SkipCloakRevert:

        if (anyReverted)
        {
            player.SendServerMessage("All equipment customizations reverted to last save point.", ColorConstants.Cyan);
        }
        else
        {
            player.SendServerMessage("No equipment changes to revert.", ColorConstants.Orange);
        }
    }

    public void ConfirmAndClose()
    {
        ClearAllBackupsFromPcKey();
        player.SendServerMessage("Equipment customization confirmed!", ColorConstants.Green);
    }

    public void CopyAppearanceToItem(NwItem targetItem, EquipmentType equipmentType)
    {
        if (!targetItem.IsValid)
        {
            player.SendServerMessage("Invalid item selected.", ColorConstants.Orange);
            return;
        }

        NwCreature? creature = player.ControlledCreature;
        if (creature == null) return;

        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                CopyWeaponAppearance(targetItem, creature);
                break;
            case EquipmentType.OffHand:
                CopyOffHandAppearance(targetItem, creature);
                break;
            case EquipmentType.Boots:
                CopyBootsAppearance(targetItem, creature);
                break;
            case EquipmentType.Helmet:
                CopyHelmetAppearance(targetItem, creature);
                break;
            case EquipmentType.Cloak:
                CopyCloakAppearance(targetItem, creature);
                break;
        }
    }

    private void CopyWeaponAppearance(NwItem targetItem, NwCreature creature)
    {
        WeaponBackupData? weaponBackup = LoadWeaponBackupFromPcKey();
        if (weaponBackup == null)
        {
            player.SendServerMessage("No weapon appearance backup found.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        // Get the currently equipped weapon to check its item class
        NwItem? currentWeapon = creature.GetItemInSlot(InventorySlot.RightHand);
        if (currentWeapon == null || !currentWeapon.IsValid)
        {
            player.SendServerMessage("No weapon currently equipped.", ColorConstants.Orange);
            return;
        }

        string currentWeaponClass = currentWeapon.BaseItem.ItemClass;
        string targetItemClass = targetItem.BaseItem.ItemClass;

        // Check if the target weapon class matches the current weapon class
        if (currentWeaponClass != targetItemClass)
        {
            player.SendServerMessage($"Selected weapon type ({targetItemClass}) does not match equipped weapon type ({currentWeaponClass}).", ColorConstants.Orange);
            return;
        }

        bool isStackable = targetItem.StackSize > 1;
        bool isSimpleModel = targetItem.BaseItem.ModelType == BaseItemModelType.Simple;

        if (isStackable)
        {
            if (isSimpleModel)
            {
                // For simple model stackable items (e.g., shurikens, darts, custom simple weapons)
                // Use ITEM_APPR_TYPE_SIMPLE_MODEL
                uint copy = NWScript.CopyItemAndModify(targetItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, weaponBackup.TopModel, 1);
                if (NWScript.GetIsObjectValid(copy) == 1)
                {
                    NwItem? weaponCopy = copy.ToNwObject<NwItem>();
                    if (weaponCopy != null && weaponCopy.IsValid)
                    {
                        float scaleValue = weaponBackup.Scale / 100f;
                        NWScript.SetObjectVisualTransform(weaponCopy, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                        targetItem.Destroy();
                        player.SendServerMessage($"Copied weapon appearance to {weaponCopy.Name}.", ColorConstants.Green);
                    }
                    else
                    {
                        player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
                    }
                }
                else
                {
                    player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
                }
            }
            else
            {
                // Composite/Complex stackable weapons (e.g., throwing axes)
                // CopyItemAndModify doesn't work for stackable WEAPON_MODEL types
                // Use waypoint workaround to prevent stack merging

                // Store original stack size
                int originalStackSize = targetItem.StackSize;

                // Find the ds_copy waypoint in core_atr area
                NwArea? targetArea = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == "core_atr");
                if (targetArea == null)
                {
                    player.SendServerMessage("Failed to find copy area.", ColorConstants.Red);
                    return;
                }

                NwWaypoint? copyWaypoint = targetArea.FindObjectsOfTypeInArea<NwWaypoint>().FirstOrDefault(w => w.Tag == "ds_copy");
                if (copyWaypoint == null)
                {
                    player.SendServerMessage("Failed to find copy waypoint.", ColorConstants.Red);
                    return;
                }

                // Create copy at the waypoint (prevents stack merging with original)
                uint copyId = NWScript.CopyItem(targetItem, copyWaypoint, 1);

                if (NWScript.GetIsObjectValid(copyId) == 0)
                {
                    player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
                    return;
                }

                NwItem? copiedWeapon = copyId.ToNwObject<NwItem>();
                if (copiedWeapon == null || !copiedWeapon.IsValid)
                {
                    NWScript.DestroyObject(copyId);
                    player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
                    return;
                }

                // Set the stack size on the copy
                copiedWeapon.StackSize = originalStackSize;

                // Apply the weapon appearance from backup
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)weaponBackup.TopModel);
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)weaponBackup.MidModel);
                copiedWeapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)weaponBackup.BotModel);

                // Apply scale
                float scaleValue = weaponBackup.Scale / 100f;
                NWScript.SetObjectVisualTransform(copiedWeapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                // Destroy the original
                targetItem.Destroy();

                // Move the copy from waypoint to player
                NwItem? newWeapon = copiedWeapon.Clone(creature);
                if (newWeapon == null || !newWeapon.IsValid)
                {
                    player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
                    return;
                }

                // Destroy the waypoint copy
                copiedWeapon.Destroy();

                player.SendServerMessage($"Copied weapon appearance to {newWeapon.Name}.", ColorConstants.Green);
            }
        }
        else
        {
            // For non-stackable items, use clone
            NwItem weaponClone = targetItem.Clone(creature);

            if (weaponClone.IsValid)
            {
                weaponBackup.ApplyToItem(weaponClone);
                targetItem.Destroy();
                player.SendServerMessage($"Copied weapon appearance to {weaponClone.Name}.", ColorConstants.Green);
            }
            else
            {
                player.SendServerMessage("Failed to copy weapon appearance.", ColorConstants.Red);
            }
        }
    }

    private void CopyOffHandAppearance(NwItem targetItem, NwCreature creature)
    {
        OffHandBackupData? offHandBackup = LoadOffHandBackupFromPcKey();
        if (offHandBackup == null)
        {
            player.SendServerMessage("No off-hand appearance backup found.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        // Get the currently equipped off-hand item
        NwItem? currentOffHand = creature.GetItemInSlot(InventorySlot.LeftHand);
        if (currentOffHand == null || !currentOffHand.IsValid)
        {
            player.SendServerMessage("No off-hand item currently equipped.", ColorConstants.Orange);
            return;
        }

        string currentItemClass = currentOffHand.BaseItem.ItemClass;
        string targetItemClass = targetItem.BaseItem.ItemClass;

        // Check if the target item class matches the current item class
        if (currentItemClass != targetItemClass)
        {
            player.SendServerMessage($"Selected item type ({targetItemClass}) does not match equipped off-hand type ({currentItemClass}).", ColorConstants.Orange);
            return;
        }

        if (offHandBackup.IsSimple)
        {
            // For simple items, use CopyItemAndModify (preserves stack size automatically)
            uint copy = NWScript.CopyItemAndModify(targetItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, offHandBackup.TopModel, 1);
            if (NWScript.GetIsObjectValid(copy) == 1)
            {
                NwItem? copiedItem = copy.ToNwObject<NwItem>();
                if (copiedItem != null && copiedItem.IsValid)
                {
                    // Apply scale
                    float scaleValue = offHandBackup.Scale / 100f;
                    NWScript.SetObjectVisualTransform(copiedItem, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                    targetItem.Destroy();
                    player.SendServerMessage($"Copied off-hand appearance to {copiedItem.Name}.", ColorConstants.Green);
                }
            }
            else
            {
                player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
            }
        }
        else
        {
            // For complex items (weapons/shields)
            bool isStackable = targetItem.StackSize > 1;

            if (isStackable)
            {
                // Use CopyItemAndModify for stackable items
                uint copy1 = NWScript.CopyItemAndModify(targetItem, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_TOP, offHandBackup.TopModel, 1);
                if (NWScript.GetIsObjectValid(copy1) == 1)
                {
                    uint copy2 = NWScript.CopyItemAndModify(copy1, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_MIDDLE, offHandBackup.MidModel, 1);
                    NWScript.DestroyObject(copy1);

                    if (NWScript.GetIsObjectValid(copy2) == 1)
                    {
                        uint copy3 = NWScript.CopyItemAndModify(copy2, NWScript.ITEM_APPR_TYPE_WEAPON_MODEL, NWScript.ITEM_APPR_WEAPON_MODEL_BOTTOM, offHandBackup.BotModel, 1);
                        NWScript.DestroyObject(copy2);

                        if (NWScript.GetIsObjectValid(copy3) == 1)
                        {
                            NwItem? offHandCopy = copy3.ToNwObject<NwItem>();
                            if (offHandCopy != null && offHandCopy.IsValid)
                            {
                                float scaleValue = offHandBackup.Scale / 100f;
                                NWScript.SetObjectVisualTransform(offHandCopy, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);

                                targetItem.Destroy();
                                player.SendServerMessage($"Copied off-hand appearance to {offHandCopy.Name}.", ColorConstants.Green);
                            }
                            else
                            {
                                player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
                            }
                        }
                        else
                        {
                            player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
                        }
                    }
                    else
                    {
                        player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
                    }
                }
                else
                {
                    player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
                }
            }
            else
            {
                // For non-stackable items, use clone
                NwItem offHandClone = targetItem.Clone(creature);

                if (offHandClone.IsValid)
                {
                    offHandBackup.ApplyToItem(offHandClone);
                    targetItem.Destroy();
                    player.SendServerMessage($"Copied off-hand appearance to {offHandClone.Name}.", ColorConstants.Green);
                }
                else
                {
                    player.SendServerMessage("Failed to copy off-hand appearance.", ColorConstants.Red);
                }
            }
        }
    }

    private void CopyBootsAppearance(NwItem targetItem, NwCreature creature)
    {
        BootsBackupData? bootsBackup = LoadBootsBackupFromPcKey();
        if (bootsBackup == null)
        {
            player.SendServerMessage("No boots appearance backup found.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        // Check if it's boots by checking the item class
        string itemClass = targetItem.BaseItem.ItemClass;
        if (itemClass != "it_boots")
        {
            player.SendServerMessage("Selected item is not boots.", ColorConstants.Orange);
            return;
        }

        // Clone the target item and apply the backup appearance to the clone
        NwItem bootsClone = targetItem.Clone(creature);

        if (bootsClone.IsValid)
        {
            // Apply the backup appearance to the cloned item
            bootsBackup.ApplyToItem(bootsClone);

            // Destroy the original item
            targetItem.Destroy();

            player.SendServerMessage($"Copied boots appearance to {bootsClone.Name}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to copy boots appearance.", ColorConstants.Red);
        }
    }

    private void CopyHelmetAppearance(NwItem targetItem, NwCreature creature)
    {
        HelmetBackupData? helmetBackup = LoadHelmetBackupFromPcKey();
        if (helmetBackup == null)
        {
            player.SendServerMessage("No helmet appearance backup found.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        // Check if it's a helmet by checking item class
        string itemClass = targetItem.BaseItem.ItemClass;
        if (itemClass != "helm")
        {
            player.SendServerMessage("Selected item is not a helmet.", ColorConstants.Orange);
            return;
        }

        // Apply the backup appearance using CopyItemAndModify
        uint copy = NWScript.CopyItemAndModify(targetItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, helmetBackup.Appearance, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NwItem? helmetCopy = copy.ToNwObject<NwItem>();

            // Apply all color channels
            for (int i = 0; i < 6; i++)
            {
                int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                uint colorCopy = NWScript.CopyItemAndModify(helmetCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, helmetBackup.Colors[i], 1);
                if (NWScript.GetIsObjectValid(colorCopy) == 1)
                {
                    if (helmetCopy != null && helmetCopy.IsValid) NWScript.DestroyObject(helmetCopy);
                    helmetCopy = colorCopy.ToNwObject<NwItem>();
                }
            }

            // Destroy the original target item and keep the modified copy
            NWScript.DestroyObject(targetItem);

            player.SendServerMessage($"Copied helmet appearance to {helmetCopy?.Name ?? "helmet"}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to copy helmet appearance.", ColorConstants.Red);
        }
    }

    private void CopyCloakAppearance(NwItem targetItem, NwCreature creature)
    {
        CloakBackupData? cloakBackup = LoadCloakBackupFromPcKey();
        if (cloakBackup == null)
        {
            player.SendServerMessage("No cloak appearance backup found.", ColorConstants.Orange);
            return;
        }

        // Check if the player owns the target item
        if (targetItem.Possessor != null && targetItem.Possessor.ObjectId != player.ControlledCreature?.ObjectId)
        {
            player.SendServerMessage("That item doesn't belong to you. Select an item from your inventory.", ColorConstants.Orange);
            return;
        }

        // Check if it's a cloak by checking item class
        string itemClass = targetItem.BaseItem.ItemClass;
        if (itemClass != "cloak")
        {
            player.SendServerMessage("Selected item is not a cloak.", ColorConstants.Orange);
            return;
        }

        // Apply the backup appearance using CopyItemAndModify
        uint copy = NWScript.CopyItemAndModify(targetItem, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0, cloakBackup.Appearance, 1);
        if (NWScript.GetIsObjectValid(copy) == 1)
        {
            NwItem? cloakCopy = copy.ToNwObject<NwItem>();

            // Apply all color channels
            for (int i = 0; i < 6; i++)
            {
                int colorType = NWScript.ITEM_APPR_ARMOR_COLOR_LEATHER1 + i;
                uint colorCopy = NWScript.CopyItemAndModify(cloakCopy, NWScript.ITEM_APPR_TYPE_ARMOR_COLOR, colorType, cloakBackup.Colors[i], 1);
                if (NWScript.GetIsObjectValid(colorCopy) == 1)
                {
                    if (cloakCopy != null && cloakCopy.IsValid) NWScript.DestroyObject(cloakCopy);
                    cloakCopy = colorCopy.ToNwObject<NwItem>();
                }
            }

            // Destroy the original target item and keep the modified copy
            NWScript.DestroyObject(targetItem);

            player.SendServerMessage($"Copied cloak appearance to {cloakCopy?.Name ?? "cloak"}.", ColorConstants.Green);
        }
        else
        {
            player.SendServerMessage("Failed to copy cloak appearance.", ColorConstants.Red);
        }
    }

    private void SaveAllBackupsToPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return;

        // Save weapon backup
        if (_currentWeapon != null && _currentWeapon.IsValid)
        {
            WeaponBackupData weaponBackup = WeaponBackupData.FromItem(_currentWeapon);
            string json = JsonConvert.SerializeObject(weaponBackup);
            NWScript.SetLocalString(pcKey, WeaponBackupKey, json);
        }

        // Save off-hand backup
        if (_currentOffHand != null && _currentOffHand.IsValid)
        {
            OffHandBackupData offHandBackup = OffHandBackupData.FromItem(_currentOffHand);
            string json = JsonConvert.SerializeObject(offHandBackup);
            NWScript.SetLocalString(pcKey, OffHandBackupKey, json);
        }

        // Save boots backup
        if (_currentBoots != null && _currentBoots.IsValid)
        {
            BootsBackupData bootsBackup = BootsBackupData.FromItem(_currentBoots);
            string json = JsonConvert.SerializeObject(bootsBackup);
            NWScript.SetLocalString(pcKey, BootsBackupKey, json);
        }

        // Save helmet backup
        if (_currentHelmet != null && _currentHelmet.IsValid)
        {
            HelmetBackupData helmetBackup = HelmetBackupData.FromItem(_currentHelmet);
            string json = JsonConvert.SerializeObject(helmetBackup);
            NWScript.SetLocalString(pcKey, HelmetBackupKey, json);
        }

        // Save cloak backup
        if (_currentCloak != null && _currentCloak.IsValid)
        {
            CloakBackupData cloakBackup = CloakBackupData.FromItem(_currentCloak);
            string json = JsonConvert.SerializeObject(cloakBackup);
            NWScript.SetLocalString(pcKey, CloakBackupKey, json);
        }
    }

    private WeaponBackupData? LoadWeaponBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return null;

        string json = NWScript.GetLocalString(pcKey, WeaponBackupKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<WeaponBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private OffHandBackupData? LoadOffHandBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return null;

        string json = NWScript.GetLocalString(pcKey, OffHandBackupKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<OffHandBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private BootsBackupData? LoadBootsBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return null;

        string json = NWScript.GetLocalString(pcKey, BootsBackupKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<BootsBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private HelmetBackupData? LoadHelmetBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return null;

        string json = NWScript.GetLocalString(pcKey, HelmetBackupKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<HelmetBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private CloakBackupData? LoadCloakBackupFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey == null || !pcKey.IsValid) return null;

        string json = NWScript.GetLocalString(pcKey, CloakBackupKey);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonConvert.DeserializeObject<CloakBackupData>(json);
        }
        catch
        {
            return null;
        }
    }

    private void ClearAllBackupsFromPcKey()
    {
        NwItem? pcKey = player.LoginCreature?.FindItemWithTag("ds_pckey");
        if (pcKey != null && pcKey.IsValid)
        {
            NWScript.DeleteLocalString(pcKey, WeaponBackupKey);
            NWScript.DeleteLocalString(pcKey, OffHandBackupKey);
            NWScript.DeleteLocalString(pcKey, BootsBackupKey);
            NWScript.DeleteLocalString(pcKey, HelmetBackupKey);
            NWScript.DeleteLocalString(pcKey, CloakBackupKey);
        }
    }
}

public class WeaponBackupData
{
    public int TopModel { get; set; }
    public int MidModel { get; set; }
    public int BotModel { get; set; }
    public int Scale { get; set; } = 100;
    public bool IsSimpleModel { get; set; }

    public static WeaponBackupData FromItem(NwItem weapon)
    {
        VisualTransform transform = weapon.VisualTransform;
        int scale = (int)(transform.Scale * 100);
        bool isSimple = weapon.BaseItem.ModelType == BaseItemModelType.Simple;

        if (isSimple)
        {
            // For simple model items (e.g., shurikens, darts, custom simple weapons), get the simple model appearance
            return new WeaponBackupData
            {
                TopModel = NWScript.GetItemAppearance(weapon, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0),
                MidModel = 0,
                BotModel = 0,
                Scale = scale,
                IsSimpleModel = true
            };
        }
        else
        {
            // For parts-based weapons, get the weapon model parts
            return new WeaponBackupData
            {
                TopModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top),
                MidModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle),
                BotModel = weapon.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom),
                Scale = scale,
                IsSimpleModel = false
            };
        }
    }

    public void ApplyToItem(NwItem weapon)
    {
        if (IsSimpleModel)
        {
            // For simple model items, we can't use SetWeaponModel - the caller must use CopyItemAndModify
            // This method is only used for non-stackable items in CopyWeaponAppearance
            // Just apply the scale
            float scaleValue = Scale / 100f;
            NWScript.SetObjectVisualTransform(weapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
        }
        else
        {
            weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)TopModel);
            weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)MidModel);
            weapon.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BotModel);

            float scaleValue = Scale / 100f;
            NWScript.SetObjectVisualTransform(weapon, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
        }
    }
}

public class OffHandBackupData
{
    public int TopModel { get; set; }
    public int MidModel { get; set; }
    public int BotModel { get; set; }
    public int Scale { get; set; } = 100;
    public bool IsSimple { get; set; }

    public static OffHandBackupData FromItem(NwItem offHand)
    {
        VisualTransform transform = offHand.VisualTransform;
        int scale = (int)(transform.Scale * 100);
        bool isSimple = offHand.BaseItem.ModelType == BaseItemModelType.Simple;

        OffHandBackupData backup = new OffHandBackupData
        {
            Scale = scale,
            IsSimple = isSimple
        };

        if (isSimple)
        {
            backup.TopModel = NWScript.GetItemAppearance(offHand, NWScript.ITEM_APPR_TYPE_SIMPLE_MODEL, 0);
            backup.MidModel = 1;
            backup.BotModel = 1;
        }
        else
        {
            backup.TopModel = offHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Top);
            backup.MidModel = offHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Middle);
            backup.BotModel = offHand.Appearance.GetWeaponModel(ItemAppearanceWeaponModel.Bottom);
        }

        return backup;
    }

    public void ApplyToItem(NwItem offHand)
    {
        if (!IsSimple)
        {
            offHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Top, (byte)TopModel);
            offHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Middle, (byte)MidModel);
            offHand.Appearance.SetWeaponModel(ItemAppearanceWeaponModel.Bottom, (byte)BotModel);
        }

        float scaleValue = Scale / 100f;
        NWScript.SetObjectVisualTransform(offHand, NWScript.OBJECT_VISUAL_TRANSFORM_SCALE, scaleValue);
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
        HelmetBackupData data = new HelmetBackupData
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
        CloakBackupData data = new CloakBackupData
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

