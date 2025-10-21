using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

public sealed class DmForgePresenter : ScryPresenter<DmForgeView>
{
    private const string WindowTitle = "DM Forge";
    private readonly NwPlayer _player;
    private readonly NwItem _item;
    private readonly CraftingPropertyData _propertyData;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private readonly List<CraftingProperty> _available = new();
    private readonly List<(ItemProperty ip, CraftingProperty cp, bool removable)> _current = new();
    private string _search = string.Empty;

    public DmForgePresenter(NwPlayer player, NwItem item, CraftingPropertyData propData)
    {
        _player = player;
        _item = item;
        _propertyData = propData;
        View = new DmForgeView(this);

        BuildCaches();
    }

    public override DmForgeView View { get; }

    private void BuildCaches()
    {
        _available.Clear();
        _current.Clear();

        int baseItemType = NWScript.GetBaseItemType(_item);
        bool isTwoHander = ItemTypeConstants.Melee2HWeapons().Contains(baseItemType);
        bool isCasterWeapon = NWScript.GetLocalInt(_item, ItemTypeConstants.CasterWeaponVar) == NWScript.TRUE;
        if (isCasterWeapon)
            baseItemType = isTwoHander ? CraftingPropertyData.CasterWeapon2H : CraftingPropertyData.CasterWeapon1H;

        if (_propertyData.Properties.TryGetValue(baseItemType, out var categories))
        {
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
            CraftingProperty cp = ItemPropertyHelper.ToCraftingProperty(ip);

            _current.Add((ip, cp, removable));
        }

        _available.Sort((a, b) => string.Compare(a.GuiLabel, b.GuiLabel, StringComparison.Ordinal));
        _current.Sort((a, b) => string.Compare(a.cp.GuiLabel, b.cp.GuiLabel, StringComparison.Ordinal));
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(300f, 200f, 1100f, 640f)
        };
    }

    public override void Create()
    {
        if (_window == null) InitBefore();
        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        // Watch search box to filter live
        Token().SetBindWatch(View.SearchBind, true);

        UpdateView();
    }

    public override void Close()
    {
        _token.Close();
    }

    public override NuiWindowToken Token() => _token;

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

    private void ApplyName()
    {
        string? newName = Token().GetBindValue(View.ItemName);
        if (!string.IsNullOrWhiteSpace(newName))
            _item.Name = newName!;
        UpdateView();
    }

    private void RemoveAt(int index)
    {
        if (index < 0 || index >= _current.Count) return;

        // Remove only the specific instance at this index, not all matching properties.
        var (ip, _, removable) = _current[index];
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

    private void AddAt(int index)
    {
        var visible = FilteredAvailable();
        if (index < 0 || index >= visible.Count) return;

        CraftingProperty cp = visible[index];

        // Allow duplicates for DM use.
        _item.AddItemProperty(cp, EffectDuration.Permanent);
        BuildCaches();
        UpdateView();
    }

    public override void UpdateView()
    {
        // Name
        Token().SetBindValue(View.ItemName, _item.Name);

        // Current
        Token().SetBindValue(View.CurrentCount, _current.Count);
        Token().SetBindValues(View.CurrentLabels, _current.Select(c => c.cp.GuiLabel).ToList());
        Token().SetBindValues(View.CurrentRemovable, _current.Select(c => c.removable).ToList());

        // Available (filtered)
        UpdateAvailableList();
    }

    private void UpdateAvailableList()
    {
        var visible = FilteredAvailable();
        Token().SetBindValue(View.AvailableCount, visible.Count);
        Token().SetBindValues(View.AvailableLabels, visible.Select(a => a.GuiLabel).ToList());
    }

    private List<CraftingProperty> FilteredAvailable()
    {
        if (string.IsNullOrWhiteSpace(_search)) return _available;
        string s = _search.ToLowerInvariant();

        // Match label or any tag in the hashset (case-insensitive)
        return _available.Where(a =>
        {
            bool labelHit = a.GuiLabel != null && a.GuiLabel.ToLowerInvariant().Contains(s);
            bool tagHit = a.Tags != null && a.Tags.Any(t => t != null && t.ToLowerInvariant().Contains(s));
            return labelHit || tagHit;
        }).ToList();
    }
}
