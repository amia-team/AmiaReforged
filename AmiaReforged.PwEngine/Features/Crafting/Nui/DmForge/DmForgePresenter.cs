using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.DmForge;

/// <summary>
/// Represents the presenter for the DM Forge user interface, managing the interaction between the underlying
/// crafting data and the presentation logic for the Nui Window system. This presenter is responsible for handling
/// events, maintaining the UI state, and updating the view in real-time.
/// </summary>
/// <remarks>
/// Inherits from the ScryPresenter class, providing implementation for specialized functionality needed
/// for managing the DM Forge interface.
/// </remarks>
public sealed class DmForgePresenter : ScryPresenter<DmForgeView>
{
    /// <summary>
    /// Represents the title of the window for the DM Forge interface.
    /// This constant is used as the display title for the associated NUI window.
    /// </summary>
    private const string WindowTitle = "DM Forge";

    /// <summary>
    /// Represents the player associated with this instance of the DM Forge presenter.
    /// This variable holds a reference to the <see cref="NwPlayer"/> object, which is used
    /// to interact with the player-specific functionalities such as creating and managing
    /// the NUI (Neverwinter User Interface) window for the crafting system.
    /// </summary>
    private readonly NwPlayer _player;

    /// <summary>
    /// Represents the item being manipulated or modified within the crafting system in the forge presenter.
    /// This item is associated with the player and can be used to apply, remove, or modify crafting properties.
    /// </summary>
    private readonly NwItem _item;

    /// <summary>
    /// Stores the crafting property data to be utilized within the presenter.
    /// This data provides access to a dictionary of item types mapped to their corresponding
    /// crafting property categories, enabling evaluation of applicable crafting properties
    /// based on the item type and additional context.
    /// </summary>
    private readonly CraftingPropertyData _propertyData;

    /// <summary>
    /// Represents the token associated with the NuiWindow created for the DmForgePresenter.
    /// </summary>
    /// <remarks>
    /// This token is used to manage and perform operations on the associated NuiWindow,
    /// such as updating binds, closing the window, and processing events. It is essential
    /// for interacting with the NuiWindow through the presenter.
    /// </remarks>
    private NuiWindowToken _token;

    /// <summary>
    /// Represents the NUI window instance used for this presenter.
    /// This window instance encapsulates the layout, geometry, and related bindings
    /// for the UI components of the DM Forge functionality.
    /// </summary>
    private NuiWindow? _window;

    /// <summary>
    /// Represents a collection of crafting properties available for display and interaction
    /// in the context of the DM Forge interface. The collection is populated based on
    /// the attributes of the associated item, including its type and relevant crafting data.
    /// </summary>
    /// <remarks>
    /// The list is dynamically built and managed in the <see cref="DmForgePresenter"/> class,
    /// ensuring it accurately reflects the capabilities and properties applicable to the
    /// specific item and game context. It is used to support crafting-related functionality
    /// in the UI layer.
    /// </remarks>
    /// <seealso cref="CraftingProperty"/>
    /// <seealso cref="DmForgePresenter.BuildCaches"/>
    private readonly List<CraftingProperty> _available = new();

    /// <summary>
    /// Stores the current list of item properties and their corresponding crafting properties
    /// within the context of the crafting system for the DM Forge. Each entry in the list
    /// consists of an <see cref="ItemProperty"/>, the mapped <see cref="CraftingProperty"/>,
    /// and a boolean indicating whether the property can be removed.
    /// </summary>
    private readonly List<(ItemProperty ip, CraftingProperty cp, bool removable)> _current = new();

    /// <summary>
    /// Represents the search query entered by the user in the context of the DmForge UI.
    /// This variable holds a trimmed string and is used to filter available crafting properties
    /// dynamically based on user input in the search box.
    /// </summary>
    private string _search = string.Empty;

    /// <summary>
    /// Represents the presenter for the DM Forge, responsible for managing the behavior, logic, and interaction between the view and associated data models.
    /// </summary>
    /// <remarks>
    /// This class acts as a concrete implementation of the <see cref="ScryPresenter{TView}"/> generic base class tailored for the <see cref="DmForgeView"/>.
    /// It provides various customization and interaction logic for the DM Forge window, such as initializing, handling events, and updating UI components.
    /// </remarks>
    /// <seealso cref="DmForgeView"/>
    /// <seealso cref="ScryPresenter{TView}"/>
    public DmForgePresenter(NwPlayer player, NwItem item, CraftingPropertyData propData)
    {
        _player = player;
        _item = item;
        _propertyData = propData;
        View = new DmForgeView(this);

        BuildCaches();
    }

    /// <summary>
    /// Represents the view associated with the presenter, responsible for defining the layout,
    /// bindings, and user interface components of the corresponding feature.
    /// </summary>
    /// <remarks>
    /// The View is strongly typed to the specific implementation of the presenter and is used
    /// to manage the visual aspects and data bindings for the feature it represents. Commonly
    /// overridden in derived types to customize UI behavior.
    /// </remarks>
    public override DmForgeView View { get; }

    /// <summary>
    /// Builds and updates the internal caches that store the available and current crafting properties
    /// for the specified item in the crafting interface.
    /// </summary>
    /// <remarks>
    /// This method clears and repopulates the `_available` and `_current` lists with relevant crafting
    /// properties, based on the item's type and its existing item properties. It handles determining
    /// whether an item is a two-handed weapon or caster weapon, fetching applicable crafting categories
    /// and properties, processing the item's current properties, and sorting the caches for use within
    /// the crafting view.
    /// </remarks>
    private void BuildCaches()
    {
        _available.Clear();
        _current.Clear();

        int baseItemType = NWScript.GetBaseItemType(_item);

        // Magic staffs are always treated as 1H caster weapons
        if (baseItemType == NWScript.BASE_ITEM_MAGICSTAFF)
        {
            baseItemType = CraftingPropertyData.CasterWeapon1H;
        }
        else
        {
            bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
            bool isCasterWeapon = NWScript.GetLocalInt(_item, ItemTypeConstants.CasterWeaponVar) == NWScript.TRUE;
            if (isCasterWeapon)
                baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;
        }

        IReadOnlyList<CraftingCategory>? availableCategories = null;
        if (_propertyData.Properties.TryGetValue(baseItemType, out IReadOnlyList<CraftingCategory>? categories))
        {
            availableCategories = categories; // Store for use when converting existing properties

            foreach (CraftingCategory cat in categories)
            {
                foreach (CraftingProperty p in cat.Properties)
                {
                    // Clone with zero costs for DM display; copy tag set (HashSet)
                    _available.Add(new CraftingProperty
                    {
                        GuiLabel = p.GuiLabel,
                        ItemProperty = p.ItemProperty,
                        PowerCost = 0,
                        CraftingTier = p.CraftingTier,
                        GoldCost = 0,
                        Removable = true,
                        Tags = p.Tags != null ? new HashSet<string>(p.Tags, StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    });
                }
            }
        }

        foreach (ItemProperty ip in _item.ItemProperties)
        {
            bool removable = ItemPropertyHelper.CanBeRemoved(ip) || ip.DurationType == EffectDuration.Permanent;
            CraftingProperty cp = ItemPropertyHelper.ToCraftingProperty(ip, availableCategories);

            _current.Add((ip, cp, removable));
        }

        _available.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
        _current.Sort((a, b) => string.Compare(a.cp.GuiLabel, b.cp.GuiLabel, StringComparison.Ordinal));
    }

    /// <summary>
    /// Initializes the components or data required before creating the main logic of the presenter.
    /// </summary>
    /// <remarks>
    /// This method sets up the NuiWindow instance with a predefined layout and geometry. It is called
    /// before creating the window in the main logic. The implementation ensures that dependent objects
    /// are initialized properly before usage in the presenter lifecycle.
    /// </remarks>
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(300f, 200f, 1100f, 640f)
        };
    }

    /// <summary>
    /// Initializes and creates the UI window for the associated presenter.
    /// </summary>
    /// <remarks>
    /// This method ensures that the UI window is initialized and attempts to create and associate it with the player.
    /// If the window has not been initialized, <see cref="InitBefore"/> is invoked prior to creation.
    /// Additionally, the method sets up binding watchers for dynamic interaction and updates the UI view with the latest state.
    /// </remarks>
    public override void Create()
    {
        if (_window == null) InitBefore();
        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        // Watch search box to filter live
        Token().SetBindWatch(View.SearchBind, true);

        UpdateView();
    }

    /// <summary>
    /// Closes the associated window or view represented by the current presenter.
    /// </summary>
    /// <remarks>
    /// This method is used to cleanly terminate or dismiss the active UI window
    /// associated with the presenter by invoking the close operation on the window token.
    /// </remarks>
    public override void Close()
    {
        _token.Close();
    }

    /// <summary>
    /// Retrieves the currently active NuiWindowToken associated with the presenter.
    /// This token is used to manage the associated Nui window instance and bindings.
    /// </summary>
    /// <returns>The active NuiWindowToken.</returns>
    public override NuiWindowToken Token() => _token;

    /// <summary>
    /// Processes NUI events dispatched to the DmForgePresenter.
    /// Depending on the event type and element ID, this method performs specific actions like updating list bindings,
    /// applying a name, adding or removing items, and closing the view.
    /// </summary>
    /// <param name="obj">The NUI event object containing details such as event type and element ID.</param>
    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType == NuiEventType.Watch && obj.ElementId == View.SearchBind.Key)
        {
            _search = (Token().GetBindValue(View.SearchBind) ?? string.Empty).Trim();
            UpdateAvailableList();
            return;
        }

        if (obj.EventType != NuiEventType.Click) return;

        if (obj.ElementId == DmForgeView.ApplyNameButtonId)
        {
            ApplyName();
            return;
        }

        if (obj.ElementId == View.CurrentRemoveId)
        {
            RemoveAt(obj.ArrayIndex);
            return;
        }

        if (obj.ElementId == View.AvailableAddId)
        {
            AddAt(obj.ArrayIndex);
            return;
        }

        if (obj.ElementId == DmForgeView.CloseId)
        {
            Close();
            return;
        }
    }

    /// <summary>
    /// Updates the name of the item associated with the presenter based on the value entered
    /// into the corresponding binding in the user interface. If the entered name is valid
    /// and not empty, it is applied to the item. After updating the name, the view is refreshed
    /// to reflect the changes.
    /// </summary>
    private void ApplyName()
    {
        string? newName = Token().GetBindValue(View.ItemName);
        if (!string.IsNullOrWhiteSpace(newName))
            _item.Name = newName!;
        UpdateView();
    }

    /// <summary>
    /// Removes an item property from the specified index in the existing collection,
    /// if it is removable and valid.
    /// </summary>
    /// <param name="index">The index of the item property to remove. Must be within the valid range of the collection.</param>
    private void RemoveAt(int index)
    {
        if (index < 0 || index >= _current.Count) return;

        // Remove only the specific instance at this index, not all matching properties.
        (ItemProperty ip, _, bool removable) = _current[index];
        if (!removable) return;

        if (ip.IsValid)
        {
            _item.RemoveItemProperty(ip);
        }
        else
        {
            ItemProperty? match = _item.ItemProperties.FirstOrDefault(p => ReferenceEquals(p, ip));
            if (match != null)
                _item.RemoveItemProperty(match);
        }

        BuildCaches();
        UpdateView();
    }

    /// <summary>
    /// Adds a crafting property at the specified index to the associated item and updates the view.
    /// </summary>
    /// <param name="index">
    /// The index of the crafting property to add from the filtered list of available properties.
    /// </param>
    private void AddAt(int index)
    {
        List<CraftingProperty> visible = FilteredAvailable();
        if (index < 0 || index >= visible.Count) return;

        CraftingProperty cp = visible[index];

        // Allow duplicates for DM use.
        _item.AddItemProperty(cp, EffectDuration.Permanent);
        BuildCaches();
        UpdateView();
    }

    /// <summary>
    /// Updates the current state of the associated view based on the underlying model's properties and data.
    /// </summary>
    /// <remarks>
    /// This method is responsible for refreshing the bound data within the associated view, ensuring that it reflects
    /// the latest state of the underlying model. This includes updating fields like item name, counts, labels, and
    /// additional calculated properties such as power totals. It also handles filtered updates to ensure any dynamic
    /// property changes are represented.
    /// </remarks>
    public override void UpdateView()
    {
        // Name
        Token().SetBindValue(View.ItemName, _item.Name);

        // Current
        Token().SetBindValue(View.CurrentCount, _current.Count);
        Token().SetBindValues(View.CurrentLabels, _current.Select(c => c.cp.GuiLabel).ToList());
        Token().SetBindValues(View.CurrentRemovable, _current.Select(c => c.removable).ToList());

        int totalPower = _current.Sum(c => c.cp.PowerCost);
        Token().SetBindValue(View.PowerTotal, totalPower.ToString());

        // Available (filtered)
        UpdateAvailableList();
    }

    /// Updates the list of available crafting properties displayed in the user interface.
    /// This method filters the available crafting properties based on the current user
    /// input (such as search criteria) and updates the corresponding bindings in the view
    /// to reflect the filtered results.
    /// It sets the following binding values on the view:
    /// - The total count of visible (filtered) crafting properties.
    /// - The labels of the filtered crafting properties.
    /// The filtering process relies on the `FilteredAvailable` helper method, which determines
    /// the list of crafting properties that match the current search or filtering parameters.
    private void UpdateAvailableList()
    {
        List<CraftingProperty> visible = FilteredAvailable();
        Token().SetBindValue(View.AvailableCount, visible.Count);
        Token().SetBindValues(View.AvailableLabels, visible.Select(a => a.GuiLabel).ToList());
    }

    /// Filters the available crafting properties based on the current search criteria.
    /// The method evaluates the `_available` list of crafting properties, applying the search
    /// term stored in the `_search` field (case-insensitive). The filtering is conducted
    /// against the `GuiLabel`, `Tags`, and `CraftingTier` properties of each item in the list.
    /// If the search string is null, empty, or consists only of white-space characters, the
    /// original `_available` list is returned unmodified.
    /// <returns>A filtered list of `CraftingProperty` objects that match the search criteria.</returns>
    private List<CraftingProperty> FilteredAvailable()
    {
        if (string.IsNullOrWhiteSpace(_search)) return _available;
        string s = _search.ToLowerInvariant();

        return _available.Where(a =>
        {
            bool labelHit = a.GuiLabel != null && a.GuiLabel.ToLowerInvariant().Contains(s);
            bool tagHit = a.Tags != null && a.Tags.Any(t => t != null && t.ToLowerInvariant().Contains(s));
            bool tierHit = a.CraftingTier.ToString().Contains(s, StringComparison.InvariantCultureIgnoreCase);
            return labelHit || tagHit || tierHit;
        }).ToList();
    }
}
