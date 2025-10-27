using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Nui.DmForge;
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
/// Responsible for initializing the Mythal Forge system, setting up necessary state and configurations for forges
/// used within the crafting system. This includes registering and organizing forge-related triggers and placeables.
/// </summary>
[ServiceBinding(typeof(MythalForgeInitializer))]
public class MythalForgeInitializer
{
    /// <summary>
    /// Represents the unique targeting mode identifier used for operations involving the Mythal Forge system.
    /// This constant is used for managing targeting logic and identifying the specific interaction mode
    /// within the player interaction workflow.
    /// </summary>
    private const string TargetingModeMythalForge = MythalForgeTag;

    /// <summary>
    /// Represents the local variable name used to track the targeting mode in the Mythal Forge system.
    /// This variable is utilized to determine whether a player is currently engaged in an item selection
    /// process within the forge's targeting functionality, helping to enforce logic and state transitions.
    /// </summary>
    private const string LvarTargetingMode = "targeting_mode";

    /// <summary>
    /// Represents a local integer variable used to determine whether a creature can interact with the Mythal Forge.
    /// </summary>
    /// <remarks>
    /// This variable is set to 1 when a player character enters a designated forge area, enabling the use of the forge.
    /// It is removed when the character exits the area, disabling forge interactions. The variable is used as a
    /// prerequisite check for opening the forge interface.
    /// </remarks>
    private const string CanUseForge = "CAN_USE_FORGE";

    /// <summary>
    /// Represents a local variable key used to indicate whether the Mythal Forge is in the process of closing.
    /// This variable is set to assist in managing the state of the forge, ensuring no new interactions can occur
    /// while it is transitioning to a closed state.
    /// </summary>
    private const string ForgeIsClosing = "CLOSING_FORGE";

    /// <summary>
    /// Constant string identifier used within the MythalForgeInitializer to tag objects of type NwPlaceable
    /// that represent a "mythal forge" in the game world. This tag is utilized to filter and retrieve specific
    /// objects for initialization and interaction purposes, such as enabling forge use, disabling forge use,
    /// and handling player interactions with the forge.
    /// </summary>
    private const string MythalForgeTag = "mythal_forge";

    /// <summary>
    /// Represents the tag identifier for triggers associated with the Mythal forge system.
    /// This tag is used to locate and initialize interactions with specific trigger objects
    /// that enable and disable forge functionality based on player proximity within the game world.
    /// </summary>
    private const string MythalTriggerTag = "mythal_trigger";

    /// <summary>
    /// Represents the service responsible for managing and calculating crafting budgets
    /// for the Mythal Forge system. This field is used to determine the available budget
    /// for crafting operations, based on the type of item or its specific attributes.
    /// </summary>
    private readonly CraftingBudgetService _budget;

    /// <summary>
    /// Instance of <see cref="DifficultyClassCalculator"/> used to compute skill check requirements
    /// for crafting actions in the Mythal Forge system.
    /// </summary>
    private readonly DifficultyClassCalculator _dcCalculator;

    /// <summary>
    /// Represents a dependency that provides access to crafting property data.
    /// Used to retrieve crafting properties and categories for specific base item types.
    /// </summary>
    private readonly CraftingPropertyData _propertyData;

    /// <summary>
    /// An instance of the <see cref="PropertyValidator"/> class, responsible for validating crafting properties
    /// against existing item properties and change list entries during the crafting process.
    /// </summary>
    private readonly PropertyValidator _validator;

    /// <summary>
    /// Instance of the <see cref="WindowDirector"/> class used to manage the
    /// opening and closing of windows within the application, including checking
    /// their open state and triggering related behavior. Provides essential window
    /// management functionality for the Mythal Forge system.
    /// </summary>
    private readonly WindowDirector _windowSystem;

    /// <summary>
    /// Provides initialization logic for Mythal Forges, setting up placeable objects and triggers for crafting functionality.
    /// </summary>
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
    /// Initializes the forging system by binding event handlers to the relevant in-game objects.
    /// For each placeable object tagged as a forge, subscribes to the OnUsed event to open the forge interface.
    /// For each trigger associated with a forge, subscribes to the OnEnter and OnExit events to enable and disable forge usage, respectively.
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
    /// Enables the use of the forge for a player character when they enter the forge's associated trigger area.
    /// </summary>
    /// <param name="obj">
    /// The event data for the trigger's OnEnter event, including the entering object information.
    /// </param>
    private void EnableForgeUse(TriggerEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsLoginPlayerCharacter(out NwPlayer? player)) return;
        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.SetLocalInt(character, CanUseForge, 1);
    }

    /// <summary>
    /// Disables the use of the forge for a player character when they exit a designated forge trigger area.
    /// Additionally, closes any currently open forge interface windows for the player.
    /// </summary>
    /// <param name="obj">The event data for the trigger exit event.</param>
    private void DisableForgeUse(TriggerEvents.OnExit obj)
    {
        if (!obj.ExitingObject.IsLoginPlayerCharacter(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        NWScript.DeleteLocalInt(character, CanUseForge);
        NWScript.SetLocalInt(character, ForgeIsClosing, 1);
        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));

        NWScript.DeleteLocalInt(character, ForgeIsClosing);
    }

    /// <summary>
    /// Event handler for opening the Mythal Forge UI when a player uses a forge placeable.
    /// Toggles the forge UI on and off for the player, validates player proximity, and manages
    /// targeting modes for both regular players and DMs.
    /// </summary>
    /// <param name="obj">The event arguments for the placeable usage event.</param>
    private void OpenForge(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        // Toggle close if already open
        if (_windowSystem.IsWindowOpen(player, typeof(MythalForgePresenter)))
        {
            _windowSystem.CloseWindow(player, typeof(MythalForgePresenter));
            return;
        }

        NwCreature? character = player.LoginCreature;
        if (character == null) return;

        // DM avatar path: bypass triggers/lvars and allow picking any item (including outside inventory).
        if (character.IsDMAvatar)
        {
            player.OnPlayerTarget += ValidateAndSelectAsDM;
            EnterTargetingModeForDM(player);
            return;
        }

        // Player path (unchanged proximity/closing checks)
        if (NWScript.GetLocalInt(character, CanUseForge) != 1 || NWScript.GetLocalInt(character, ForgeIsClosing) == 1)
        {
            player.FloatingTextString(message: "Get closer to the forge.", false);
            return;
        }

        player.OnPlayerTarget += ValidateAndSelectPlayer;

        EnterTargetingMode(player);

        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
    }

    /// <summary>
    /// Enables the targeting mode for the specified player to select an item from their inventory.
    /// </summary>
    /// <param name="player">The player for whom the targeting mode is initiated.</param>
    private void EnterTargetingMode(NwPlayer player)
    {
        player.FloatingTextString(message: "Pick an Item from your inventory.", false);
        player.OpenInventory();
        NWScript.SetLocalString(player.LoginCreature, LvarTargetingMode, TargetingModeMythalForge);
        NWScript.EnterTargetingMode(player.LoginCreature, NWScript.OBJECT_TYPE_ITEM);
    }

    // DM targeting mode: no lvars, allow selecting any item
    /// <summary>
    /// Enables the DM to enter targeting mode to select any item, bypassing the usual inventory constraints.
    /// </summary>
    /// <param name="player">The DM player entering targeting mode.</param>
    private void EnterTargetingModeForDM(NwPlayer player)
    {
        player.FloatingTextString(message: "DM: Select any item.", false);
        // Allow targeting of any item (not constrained by inventory token)
        NWScript.EnterTargetingMode(player.LoginCreature!, NWScript.OBJECT_TYPE_ITEM);
    }

    // Player validator (original behavior)
    /// <summary>
    /// Validates the selected target by the player and proceeds with Mythal Forge interaction if criteria are met.
    /// </summary>
    /// <param name="obj">The event object containing details about the player's target selection.</param>
    private void ValidateAndSelectPlayer(ModuleEvents.OnPlayerTarget obj)
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

            obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;

            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

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

            obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;
            NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

            return;
        }

        NWScript.DeleteLocalString(obj.Player.LoginCreature, LvarTargetingMode);

        MythalForgeView itemWindow = new(_propertyData, _budget, item, obj.Player, _validator, _dcCalculator);
        _windowSystem.OpenWindow(itemWindow.Presenter);

        obj.Player.OpenInventory();
        obj.Player.OnPlayerTarget -= ValidateAndSelectPlayer;
    }

    // DM validator: no lvar or proximity checks; no ownership restriction
    /// <summary>
    /// Validates the targeted item by a Dungeon Master (DM) and opens the DM Forge interface
    /// for the selected item if all conditions are met.
    /// </summary>
    /// <param name="obj">
    /// The event data for the OnPlayerTarget event, containing information about the
    /// player and their selected target.
    /// </param>
    /// <remarks>
    /// This method handles DM-specific conditions, such as bypassing standard ownership
    /// or proximity checks, and ensures the targeted item is supported by the crafting system.
    /// If the item is valid, the DM Forge interface is presented to the player.
    /// </remarks>
    private void ValidateAndSelectAsDM(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwItem item || !obj.TargetObject.IsValid) return;
        if (obj.Player.LoginCreature == null) return;

        int baseItemType = NWScript.GetBaseItemType(item);
        bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        bool isCasterWeapon = NWScript.GetLocalInt(item, sVarName: "CASTER_WEAPON") == NWScript.TRUE;
        if (isCasterWeapon)
            baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;

        if (!_propertyData.Properties.ContainsKey(baseItemType))
        {
            GenericWindow.Builder()
                .For()
                .SimplePopup()
                .WithPlayer(obj.Player)
                .WithTitle("DM Forge: Notice")
                .WithMessage("Item base type not mapped for forge properties.")
                .Open();

            obj.Player.OnPlayerTarget -= ValidateAndSelectAsDM;
            return;
        }

        // Open DM Forge (no costs, no mythals, duplicates allowed)
        DmForgePresenter dmPresenter = new(obj.Player, item, _propertyData);
        _windowSystem.OpenWindow(dmPresenter);

        obj.Player.OnPlayerTarget -= ValidateAndSelectAsDM;
    }
}
