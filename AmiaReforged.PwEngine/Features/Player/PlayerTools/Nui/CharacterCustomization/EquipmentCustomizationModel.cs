using Anvil.API;

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
    public EquipmentType CurrentEquipmentType { get; private set; } = EquipmentType.None;

    // Weapon
    public int WeaponTopModel { get; private set; } = 1;
    public int WeaponMidModel { get; private set; } = 1;
    public int WeaponBotModel { get; private set; } = 1;
    public int WeaponTopColor { get; private set; } = 1;
    public int WeaponMidColor { get; private set; } = 1;
    public int WeaponBotColor { get; private set; } = 1;

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
        if (weapon != null)
        {
            player.SendServerMessage($"Selected weapon: {weapon.Name}", ColorConstants.Cyan);
            // For now, use placeholder values 1-4
            WeaponTopModel = 1;
            WeaponMidModel = 1;
            WeaponBotModel = 1;
            WeaponTopColor = 1;
            WeaponMidColor = 1;
            WeaponBotColor = 1;
        }
        else
        {
            player.SendServerMessage("No weapon equipped in main hand.", ColorConstants.Orange);
        }
    }

    private void LoadBootsData()
    {
        NwItem? boots = player.ControlledCreature?.GetItemInSlot(InventorySlot.Boots);
        if (boots != null)
        {
            player.SendServerMessage($"Selected boots: {boots.Name}", ColorConstants.Cyan);
            // For now, use placeholder values 1-4
            BootsTopModel = 1;
            BootsMidModel = 1;
            BootsBotModel = 1;
            BootsTopColor = 1;
            BootsMidColor = 1;
            BootsBotColor = 1;
        }
        else
        {
            player.SendServerMessage("No boots equipped.", ColorConstants.Orange);
        }
    }

    private void LoadHelmetData()
    {
        NwItem? helmet = player.ControlledCreature?.GetItemInSlot(InventorySlot.Head);
        if (helmet != null)
        {
            player.SendServerMessage($"Selected helmet: {helmet.Name}", ColorConstants.Cyan);
            HelmetAppearance = 1;
        }
        else
        {
            player.SendServerMessage("No helmet equipped.", ColorConstants.Orange);
        }
    }

    private void LoadCloakData()
    {
        NwItem? cloak = player.ControlledCreature?.GetItemInSlot(InventorySlot.Cloak);
        if (cloak != null)
        {
            player.SendServerMessage($"Selected cloak: {cloak.Name}", ColorConstants.Cyan);
            CloakAppearance = 1;
        }
        else
        {
            player.SendServerMessage("No cloak equipped.", ColorConstants.Orange);
        }
    }

    public void AdjustWeaponTopModel(int delta)
    {
        WeaponTopModel = Math.Clamp(WeaponTopModel + delta, 1, 4);
    }

    public void AdjustWeaponMidModel(int delta)
    {
        WeaponMidModel = Math.Clamp(WeaponMidModel + delta, 1, 4);
    }

    public void AdjustWeaponBotModel(int delta)
    {
        WeaponBotModel = Math.Clamp(WeaponBotModel + delta, 1, 4);
    }

    public void AdjustWeaponTopColor(int delta)
    {
        WeaponTopColor = Math.Clamp(WeaponTopColor + delta, 1, 4);
    }

    public void AdjustWeaponMidColor(int delta)
    {
        WeaponMidColor = Math.Clamp(WeaponMidColor + delta, 1, 4);
    }

    public void AdjustWeaponBotColor(int delta)
    {
        WeaponBotColor = Math.Clamp(WeaponBotColor + delta, 1, 4);
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
        player.SendServerMessage("Changes saved.", ColorConstants.Green);
        // TODO: Implement actual item modification
    }

    public void RevertChanges()
    {
        player.SendServerMessage("Changes discarded.", ColorConstants.Orange);
        // TODO: Implement revert logic
    }
}

