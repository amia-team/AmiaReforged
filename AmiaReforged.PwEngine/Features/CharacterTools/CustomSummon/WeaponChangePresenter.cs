using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.CharacterTools.CustomSummon;

public sealed class WeaponChangePresenter(WeaponChangeView view, NwPlayer player, NwCreature targetCreature, NwItem widget)
    : ScryPresenter<WeaponChangeView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public override WeaponChangeView View { get; } = view;

    private NuiWindowToken _token;
    private List<BaseItemType> _compatibleWeaponTypes = new();
    private NwItem? _currentWeapon;

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        NuiWindow window = new NuiWindow(View.RootLayout(), "Change Weapon Type")
        {
            Geometry = new NuiRect(50f, 50f, 430f, 500f),
            Resizable = true
        };

        if (!player.TryCreateNuiWindow(window, out _token))
            return;

        InitializeBindValues();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void InitializeBindValues()
    {
        Token().SetBindValue(View.AlwaysEnabled, true);

        // Get the creature's current weapon
        _currentWeapon = targetCreature.GetItemInSlot(InventorySlot.RightHand);

        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("The creature has no weapon equipped in right hand.", ColorConstants.Orange);
            Close();
            return;
        }

        // Determine weapon wield type and get compatible types
        int wieldType = GetWeaponWieldType(_currentWeapon.BaseItem.ItemType);
        _compatibleWeaponTypes = GetCompatibleWeaponTypes(wieldType);

        if (_compatibleWeaponTypes.Count == 0)
        {
            player.SendServerMessage("No compatible weapon types found.", ColorConstants.Orange);
            Close();
            return;
        }

        // Build weapon type name list
        List<string> weaponNames = _compatibleWeaponTypes.Select(GetWeaponTypeName).ToList();

        Token().SetBindValue(View.WeaponTypeCount, weaponNames.Count);
        Token().SetBindValues(View.WeaponTypeNames, weaponNames);
        Token().SetBindValue(View.SelectedWeaponIndex, -1);

        Log.Info($"Weapon change modal opened. Current weapon: {_currentWeapon.BaseItem.ItemType}, Wield type: {wieldType}, Compatible types: {_compatibleWeaponTypes.Count}");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleClickEvent(eventData);
                break;
        }
    }

    private void HandleClickEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.ElementId)
        {
            case "wc_btn_select_weapon":
                int selectedIndex = eventData.ArrayIndex;
                Token().SetBindValue(View.SelectedWeaponIndex, selectedIndex);
                Log.Info($"Weapon type selected at index: {selectedIndex}");
                break;

            case "wc_btn_confirm":
                ConfirmWeaponChange();
                break;

            case "wc_btn_cancel":
                Close();
                break;
        }
    }

    private async void ConfirmWeaponChange()
    {
        int selection = Token().GetBindValue(View.SelectedWeaponIndex);

        if (selection < 0 || selection >= _compatibleWeaponTypes.Count)
        {
            player.SendServerMessage("No weapon type selected.", ColorConstants.Orange);
            return;
        }

        if (_currentWeapon == null || !_currentWeapon.IsValid)
        {
            player.SendServerMessage("Current weapon is invalid.", ColorConstants.Orange);
            Close();
            return;
        }

        BaseItemType newWeaponType = _compatibleWeaponTypes[selection];
        string weaponName = GetWeaponTypeName(newWeaponType);

        Log.Info($"Changing weapon from {_currentWeapon.BaseItem.ItemType} to {newWeaponType}");

        // Get the proper blueprint resref for this weapon type
        string weaponResref = GetWeaponResref(newWeaponType);
        if (string.IsNullOrEmpty(weaponResref))
        {
            player.SendServerMessage($"No blueprint found for weapon type: {weaponName}", ColorConstants.Red);
            Log.Error($"No resref mapping for weapon type {newWeaponType}");
            Close();
            return;
        }

        Log.Info($"Creating weapon with resref: {weaponResref}");

        // Create new weapon using the proper blueprint
        NwItem? newWeapon = await NwItem.Create(weaponResref, targetCreature);

        if (newWeapon == null)
        {
            player.SendServerMessage($"Failed to create weapon of type: {weaponName}", ColorConstants.Red);
            Log.Error($"Failed to create weapon of type {newWeaponType} with resref {weaponResref}");
            Close();
            return;
        }

        // Copy properties from old weapon to new weapon
        int propertiesCopied = 0;
        foreach (var prop in _currentWeapon.ItemProperties)
        {
            // Clone the item property by recreating it
            newWeapon.AddItemProperty(prop, EffectDuration.Permanent);
            propertiesCopied++;
        }

        Log.Info($"Copied {propertiesCopied} properties to new weapon");

        // Unequip and destroy old weapon
        targetCreature.RunUnequip(_currentWeapon);
        _currentWeapon.Destroy();

        // Give new weapon to creature and equip it
        targetCreature.AcquireItem(newWeapon);
        targetCreature.RunEquip(newWeapon, InventorySlot.RightHand);

        player.SendServerMessage($"Changed weapon to {weaponName.ColorString(ColorConstants.Cyan)}. Copied {propertiesCopied} properties.", ColorConstants.Green);
        Log.Info($"Weapon change completed successfully for {targetCreature.Name}");

        Close();
    }

    private int GetWeaponWieldType(BaseItemType baseItemType)
    {
        // Based on baseitems.2da WeaponWield column
        // 0 = one-handed, 4 = two-handed, 5 = bow, 6 = crossbow, 8 = double-sided, 10 = dart/sling, 11 = shuriken/throwing axe
        return baseItemType switch
        {
            // One-handed weapons (0)
            BaseItemType.Longsword or BaseItemType.Shortsword or BaseItemType.Rapier or
            BaseItemType.Scimitar or BaseItemType.Handaxe or BaseItemType.Battleaxe or
            BaseItemType.LightHammer or BaseItemType.LightMace or BaseItemType.Morningstar or
            BaseItemType.Club or BaseItemType.Dagger or BaseItemType.Kama or
            BaseItemType.Kukri or BaseItemType.Sickle or BaseItemType.Warhammer or
            BaseItemType.LightFlail or BaseItemType.Whip or BaseItemType.Trident or
            BaseItemType.DwarvenWaraxe or BaseItemType.Bastardsword or BaseItemType.Katana or
            BaseItemType.MagicStaff => 0,

            // Two-handed weapons (4)
            BaseItemType.Greatsword or BaseItemType.Greataxe or BaseItemType.Halberd or
            BaseItemType.HeavyFlail or BaseItemType.Scythe or BaseItemType.ShortSpear => 4,
            BaseItemType.Quarterstaff => 4,

            // Bows (5)
            BaseItemType.Longbow or BaseItemType.Shortbow => 5,

            // Crossbows (6)
            BaseItemType.LightCrossbow or BaseItemType.HeavyCrossbow => 6,

            // Double-sided weapons (8)
            BaseItemType.Doubleaxe or BaseItemType.TwoBladedSword or BaseItemType.DireMace => 8,

            // Thrown light (10)
            BaseItemType.Dart or BaseItemType.Sling => 10,

            // Thrown (11)
            BaseItemType.Shuriken or BaseItemType.ThrowingAxe => 11,

            _ => -1 // Unknown
        };
    }

    private List<BaseItemType> GetCompatibleWeaponTypes(int wieldType)
    {
        List<BaseItemType> compatible = new();

        if (wieldType < 0)
            return compatible;

        // Determine which wield types are compatible
        List<int> compatibleWieldTypes = new() { wieldType };

        // Special compatibility rules:
        // Two-handed (4) ↔ Double-sided (8)
        if (wieldType == 4)
        {
            compatibleWieldTypes.Add(8); // Two-handed can be changed to double-sided
        }
        else if (wieldType == 8)
        {
            compatibleWieldTypes.Add(4); // Double-sided can be changed to two-handed
        }
        // Bow (5) ↔ Crossbow (6)
        else if (wieldType == 5)
        {
            compatibleWieldTypes.Add(6); // Bow can be changed to crossbow
        }
        else if (wieldType == 6)
        {
            compatibleWieldTypes.Add(5); // Crossbow can be changed to bow
        }

        // Add all weapon types that match any compatible wield type
        foreach (BaseItemType weaponType in Enum.GetValues(typeof(BaseItemType)))
        {
            int weaponWieldType = GetWeaponWieldType(weaponType);
            if (compatibleWieldTypes.Contains(weaponWieldType))
            {
                compatible.Add(weaponType);
            }
        }

        return compatible;
    }

    private string GetWeaponTypeName(BaseItemType weaponType)
    {
        // Convert enum name to friendly display name
        string name = weaponType.ToString();

        // Add spaces before capital letters
        return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
    }

    private string GetWeaponResref(BaseItemType weaponType)
    {
        // Map BaseItemType to standard NWN blueprint resrefs
        return weaponType switch
        {
            // One-handed weapons
            BaseItemType.Longsword => "js_bla_wels",
            BaseItemType.Shortsword => "js_bla_wess",
            BaseItemType.Rapier => "js_bla_wera",
            BaseItemType.Scimitar => "js_bla_wesc",
            BaseItemType.Handaxe => "js_bla_weha",
            BaseItemType.Battleaxe => "js_bla_weba",
            BaseItemType.LightHammer => "js_bla_welh",
            BaseItemType.LightMace => "js_bla_wema",
            BaseItemType.Morningstar => "js_bla_wemo",
            BaseItemType.Club => "js_bla_wecl",
            BaseItemType.Dagger => "js_bla_weda",
            BaseItemType.Kama => "js_bla_wekm",
            BaseItemType.Kukri => "js_bla_weku",
            BaseItemType.Sickle => "js_bla_wesi",
            BaseItemType.Warhammer => "js_bla_wewa",
            BaseItemType.LightFlail => "js_bla_welf",
            BaseItemType.Whip => "js_bla_wewh",
            BaseItemType.Trident => "js_bla_wetr",
            BaseItemType.DwarvenWaraxe => "js_bla_wedw",
            BaseItemType.Bastardsword => "js_bla_webs",
            BaseItemType.Katana => "js_bla_weka",
            BaseItemType.MagicStaff => "js_bla_wems",

            // Two-handed weapons
            BaseItemType.Greatsword => "js_bla_wegs",
            BaseItemType.Greataxe => "js_bla_wega",
            BaseItemType.Halberd => "js_bla_wehb",
            BaseItemType.HeavyFlail => "js_bla_wehf",
            BaseItemType.Scythe => "js_bla_wesy",
            BaseItemType.Quarterstaff => "js_bla_wequ",
            BaseItemType.ShortSpear => "js_bla_wesp",

            // Bows
            BaseItemType.Longbow => "js_arch_bow",
            BaseItemType.Shortbow => "js_arch_sbow",

            // Crossbows
            BaseItemType.LightCrossbow => "js_arch_lbow",
            BaseItemType.HeavyCrossbow => "js_arch_cbow",

            // Double-sided
            BaseItemType.Doubleaxe => "js_bla_wedb",
            BaseItemType.TwoBladedSword => "js_bla_we2b",
            BaseItemType.DireMace => "js_bla_wedm",

            // Thrown
            BaseItemType.Dart => "js_arch_dart",
            BaseItemType.Sling => "js_arch_sling",
            BaseItemType.Shuriken => "js_arch_shrk",
            BaseItemType.ThrowingAxe => "js_arch_thax",

            _ => string.Empty
        };
    }
}

