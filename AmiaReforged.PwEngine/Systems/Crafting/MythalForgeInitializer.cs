using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(MythalForgeInitializer))]
public class MythalForgeInitializer
{
    private const string TargetingModeMythalForge = "mythal_forge";
    private const string LvarTargetingMode = "targeting_mode";

    private readonly WindowDirector _windowSystem;
    private readonly CraftingPropertyData _propertyData;
    private readonly CraftingBudgetService _budget;
    private readonly PropertyValidator _validator;
    private readonly DifficultyClassCalculator _dcCalculator;

    public MythalForgeInitializer(WindowDirector windowSystem, CraftingPropertyData propertyData,
        CraftingBudgetService budget, PropertyValidator validator, DifficultyClassCalculator dcCalculator)
    {
        _windowSystem = windowSystem;
        _propertyData = propertyData;
        _budget = budget;
        _validator = validator;
        _dcCalculator = dcCalculator;

        InitForges();
    }

    private void InitForges()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>("mythal_forge");

        foreach (NwPlaceable nwPlaceable in forges)
        {
            nwPlaceable.OnUsed += OpenForge;
        }
    }

    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        string environment = UtilPlugin.GetEnvironmentVariable("SERVER_MODE");
        
        if(environment == "live") return;
        
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
        {
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));
            return;
        }

        player.OnPlayerTarget += ValidateAndSelect;

        EnterTargetingMode(player);

        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    private void EnterTargetingMode(NwPlayer player)
    {
        player.FloatingTextString("Pick an Item from your inventory.", false);
        player.OpenInventory();
        NWScript.EnterTargetingMode(player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    private void ValidateAndSelect(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if (obj.Player.LoginCreature == null) return;

        if (NWScript.GetLocalString(obj.Player.LoginCreature, LvarTargetingMode) != TargetingModeMythalForge) return;

        int isCasterWeapon = NWScript.GetLocalInt(item, "CASTER_WEAPON");
        int baseItemType = NWScript.GetBaseItemType(item);
        
        bool is2H = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        if (isCasterWeapon == NWScript.TRUE)
        {
            baseItemType = is2H ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;
        }
        

        bool notFound =
            !_propertyData.Properties.TryGetValue(baseItemType, out IReadOnlyList<CraftingCategory>? categories);
        if (notFound)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle("Mythal Forge: Notice")
                .WithMessage("Item not supported by Mythal forge")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelect;

            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            //  Closes the inventory window
            obj.Player.OpenInventory();

            return;
        }

        if (item.Possessor != null && item.Possessor.ObjectId != obj.Player.LoginCreature.ObjectId)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle("Mythal Forge: Notice")
                .WithMessage("That doesn't belong to you. Pick an item from your inventory.")
                .Open();
            obj.Player.OpenInventory();
            return;
        }

        if (categories == null)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle("Mythal Forge: Error")
                .WithMessage(
                    "Item supported by the Mythal forge, but has no properties. This is a bug and should be reported.")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelect;
            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            return;
        }


        // Remove the token.
        NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

        MythalForgeView itemWindow = new(_propertyData, _budget, item, obj.Player, _validator, _dcCalculator);
        _windowSystem.OpenWindow(itemWindow.Presenter);

        obj.Player.OpenInventory();
        obj.Player.OnPlayerTarget -= ValidateAndSelect;
    }
}