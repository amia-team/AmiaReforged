using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Crafting;

/// <summary>
///     Initializes the Mythal Forge system at startup. Responsible for listening to player interactions with the forge and
///     its triggers.
/// </summary>
[ServiceBinding(typeof(MythalForgeInitializer))]
public class MythalForgeInitializer
{
    private const string TargetingModeMythalForge = MythalForgeTag;
    private const string LvarTargetingMode = "targeting_mode";
    private const string CanUseForge = "CAN_USE_FORGE";
    private const string ForgeIsClosing = "CLOSING_FORGE";

    private const string MythalForgeTag = "mythal_forge";
    private const string MythalTriggerTag = "mythal_trigger";
    private readonly CraftingBudgetService _budget;
    private readonly DifficultyClassCalculator _dcCalculator;
    private readonly CraftingPropertyData _propertyData;
    private readonly PropertyValidator _validator;

    private readonly WindowDirector _windowSystem;

    /// <summary>
    ///     Dependency injected by the DI container. Do not use this constructor directly.
    /// </summary>
    /// <param name="windowSystem"></param>
    /// <param name="propertyData"></param>
    /// <param name="budget"></param>
    /// <param name="validator"></param>
    /// <param name="dcCalculator"></param>
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

    /// <summary>
    ///     Looks for all forges and triggers in the module and sets up event listeners.
    /// </summary>
    private void InitForges()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>(MythalForgeTag);

        foreach (NwPlaceable nwPlaceable in forges)
        {
            nwPlaceable.OnUsed += OpenForge;
        }

        IEnumerable<NwTrigger> forgeTriggers = NwObject.FindObjectsWithTag<NwTrigger>(MythalTriggerTag);

        foreach (NwTrigger trigger in forgeTriggers)
        {
            trigger.OnEnter += EnableForgeUse;
            trigger.OnExit += DisableForgeUse;
        }
    }

    /// <summary>
    ///     Sets a local int on the player's character to indicate they are near a forge, so they can use it.
    /// </summary>
    /// <param name="obj"></param>
    private void EnableForgeUse(TriggerEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.SetLocalInt(character, CanUseForge, 1);
    }

    /// <summary>
    ///     Unsets the local int on the player's character to indicate they are no longer near a forge.
    /// </summary>
    /// <param name="obj"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void DisableForgeUse(TriggerEvents.OnExit obj)
    {
        if (!obj.ExitingObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.DeleteLocalInt(character, CanUseForge);
        NWScript.SetLocalInt(character, ForgeIsClosing, 1);
        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));

        NWScript.DeleteLocalInt(character, ForgeIsClosing);
    }

    /// <summary>
    ///     Sets up the targeting mode for the player to select an item from their inventory to use with the forge.
    /// </summary>
    /// <param name="obj"></param>
    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
        {
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));
            return;
        }

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        if (NWScript.GetLocalInt(character, CanUseForge) != 1 || NWScript.GetLocalInt(character, ForgeIsClosing) == 1)
        {
            player.FloatingTextString(message: "Get closer to the forge.", false);
            return;
        }

        player.OnPlayerTarget += ValidateAndSelect;

        EnterTargetingMode(player);

        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    /// <summary>
    ///     Officially enters the targeting mode for the player to select an item from their inventory. Sets a local string on
    ///     the player's character to indicate they are in the Mythal Forge targeting mode.
    /// </summary>
    /// <param name="player"></param>
    private void EnterTargetingMode(NwPlayer player)
    {
        player.FloatingTextString(message: "Pick an Item from your inventory.", false);
        player.OpenInventory();
        NWScript.EnterTargetingMode(player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    /// <summary>
    ///     Targeting mode listener for the player to select an item from their inventory. Validates the item and opens the
    ///     Mythal Forge window.
    ///     Makes sure to check if the player is still in the Mythal Forge targeting mode to avoid conflicts with other systems
    ///     that use targeting mode.
    /// </summary>
    /// <param name="obj"></param>
    private void ValidateAndSelect(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if (obj.Player.LoginCreature == null) return;

        if (NWScript.GetLocalString(obj.Player.LoginCreature, LvarTargetingMode) != TargetingModeMythalForge) return;

        int baseItemType = NWScript.GetBaseItemType(item);

        bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        bool isCasterWeapon = NWScript.GetLocalInt(item, sVarName: "CASTER_WEAPON") == NWScript.TRUE;
        if (isCasterWeapon)
            baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;

        bool itemListingNotFound =
            !_propertyData.Properties.TryGetValue(baseItemType, out IReadOnlyList<CraftingCategory>? categories);
        if (itemListingNotFound)
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle(title: "Mythal Forge: Notice")
                .WithMessage(message: "Item not supported by Mythal forge")
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
                .WithTitle(title: "Mythal Forge: Notice")
                .WithMessage(message: "That doesn't belong to you. Pick an item from your inventory.")
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
                .WithTitle(title: "Mythal Forge: Error")
                .WithMessage(
                    message:
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
