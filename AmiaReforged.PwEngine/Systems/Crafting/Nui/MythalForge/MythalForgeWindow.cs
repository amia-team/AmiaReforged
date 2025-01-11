using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalForgeWindow : IWindow
{
    private readonly List<CraftingCategory> _categories = new();
    private readonly List<ChangelistEntry> _changeList = new();
    public string Title => "Mythal Forge";
    private NuiWindowToken _token;

    private readonly NwPlayer _player;
    private readonly NwItem _selection;
    private readonly CraftingPropertyData _data;
    private readonly CraftingBudgetService _budgetService;
    private readonly Dictionary<string, CraftingProperty> _itemProperties = new();

    private NuiWindow _window;

    // Buttons
    private NuiButton _applyNameButton;


    private const float AutoCloseDistance = 10.0f;
    private const string RemoveChangeId = "remove_change";
    private const string? RemoveItemProperty = "remove_item_property";

    private NuiBind<string> SpentPowers { get; } = new("spent_powers");
    private NuiBind<string> ItemName { get; } = new("item_name");

    private NuiBind<int> ItemPropertyCount { get; } = new("item_property_count");

    private NuiBind<string> ItemPropertyNames { get; } = new("item_property_labels");
    private NuiBind<string> ItemPropertyPowers { get; } = new("item_property_powers");
    private NuiBind<Color> ItemPropertyColors { get; } = new("item_property_colors");

    private NuiBind<string> EntryLabels { get; } = new("entries");
    private NuiBind<string> EntryPowerCosts { get; } = new("entry_costs");
    private NuiBind<int> EntryCount { get; } = new("entry_count");
    private NuiBind<bool> Removables { get; } = new("removables");

    private readonly List<ItemProperty> _existingProperties = new();
    private readonly List<ItemProperty> _visibleProperties = new();
    private readonly List<ItemProperty> _removedProperties = new();
    private CraftingCategorySectionView _craftingCategorySectionView;

    public NuiBind<bool> PropertyEnabled { get; } = new("property_enabled");

    public MythalForgeWindow(NwPlayer player, NwItem selection, CraftingPropertyData data,
        CraftingBudgetService budgetService)
    {
        _player = player;
        _selection = selection;
        _data = data;
        _budgetService = budgetService;

        _existingProperties.AddRange(selection.ItemProperties);

        int baseItem = NWScript.GetBaseItemType(_selection);
        if (!_data.Properties.ContainsKey(baseItem))
        {
            _player.SendServerMessage("This item cannot be modified.", ColorConstants.Red);
            return;
        }

        foreach (CraftingCategory category in data.Properties[baseItem])
        {
            _categories.Add(category);
        }

        _craftingCategorySectionView = new CraftingCategorySectionView(this, _categories);
        
        foreach (CraftingProperty property in _categories.SelectMany(element => element.Properties))
        {
            if (property.Button.Id == null) continue;
            _itemProperties.Add(property.Button.Id, property);
        }
    }

    public NuiWindowToken GetToken()
    {
        return _token;
    }

    public void Init()
    {
        NwModule.Instance.OnNuiEvent += OnNuiEvent;

        List<NuiListTemplateCell> itemPropertyCells = new()
        {
            new NuiListTemplateCell(new NuiLabel(ItemPropertyNames)),
            new NuiListTemplateCell(new NuiLabel(ItemPropertyPowers)),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = RemoveItemProperty,
                Tooltip = "Remove this property",
                Enabled = Removables
            })
        };
        NuiRow itemPropertySection = new()
        {
            Children =
            {
                new NuiList(itemPropertyCells, ItemPropertyCount)
            },
            Width = 400f,
            Height = 400f
        };


        NuiElement categorySection = _craftingCategorySectionView.GetElement();

        List<NuiListTemplateCell> changelistSectionCells = new()
        {
            new NuiListTemplateCell(new NuiLabel(EntryLabels)
            {
                ForegroundColor = ItemPropertyColors
            }),
            new NuiListTemplateCell(new NuiLabel(EntryPowerCosts)),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = "remove_change"
            })
        };

        NuiColumn changelistSection = new()
        {
            Children =
            {
                new NuiList(changelistSectionCells, EntryCount)
                {
                    RowHeight = 45f,
                    Width = 400f,
                    Height = 400f
                }
            }
        };

        NuiColumn root = new()
        {
            Id = "root_column",
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit("Edit Name", ItemName, 100, false)
                        {
                            Width = 280f,
                            Height = 60f
                        },

                        new NuiButton("Change Name")
                        {
                            Id = "apply_name",
                            Height = 60f
                        }.Assign(out _applyNameButton),
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiSpacer(),
                        new NuiSpacer(),
                        new NuiGroup
                        {
                            Element = new NuiRow
                            {
                                Children =
                                {
                                    new NuiLabel("Max:")
                                    {
                                        HorizontalAlign = NuiHAlign.Center,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiLabel(_budgetService.MythalBudgetForNwItem(_selection).ToString())
                                    {
                                        HorizontalAlign = NuiHAlign.Center,
                                        VerticalAlign = NuiVAlign.Middle
                                    }
                                }
                            },
                            Width = 200f,
                            Height = 60f
                        },
                        new NuiGroup
                        {
                            Element = new NuiRow
                            {
                                Children =
                                {
                                    new NuiLabel("Free:")
                                    {
                                        HorizontalAlign = NuiHAlign.Center,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiLabel(SpentPowers)
                                    {
                                        HorizontalAlign = NuiHAlign.Center,
                                        VerticalAlign = NuiVAlign.Middle
                                    }
                                }
                            },
                            Width = 200f,
                            Height = 60f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        categorySection,
                        itemPropertySection,
                        changelistSection
                    }
                }
            }
        };

        _window = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(400, 400, 1000f, 1000f)
        };
    }


    public void Create()
    {
        _player.TryCreateNuiWindow(_window, out _token);

        UpdateItemName();
        UpdatePropertyList();
        UpdateSpentPowers();
        UpdateSelectableProperties();
    }

    private void UpdateSpentPowers()
    {
        int spentPowers = 0;
        List<CraftingProperty> craftingProperties = _categories.SelectMany(c => c.Properties).ToList();
        foreach (ItemProperty p in _selection.ItemProperties)
        {
            if (craftingProperties.Any(c => c.GameLabel == ItemPropertyHelper.GameLabel(p)))
            {
                CraftingProperty property =
                    craftingProperties.First(c => c.GameLabel == ItemPropertyHelper.GameLabel(p));
                spentPowers += property.PowerCost;
            }
            else
            {
                spentPowers += 2;
            }
        }

        foreach (ChangelistEntry e in _changeList)
        {
            if (e.State == ChangeState.Added)
            {
                spentPowers += e.Property.PowerCost;
            }
            else if (e.State == ChangeState.Removed)
            {
                spentPowers -= e.Property.PowerCost;
            }
        }

        _token.SetBindValue(SpentPowers, spentPowers.ToString());
    }

    private void UpdateSelectableProperties()
    {
        string? spentPowersString = _token.GetBindValue(SpentPowers);
        if (spentPowersString == null) return;

        int spentPowers = int.Parse(spentPowersString);
        int maxBudget = _budgetService.MythalBudgetForNwItem(_selection);
        int remainingBudget = maxBudget - spentPowers;

        List<bool> enabled;
        if (remainingBudget < 0)
        {
            enabled = _categories.SelectMany(category => category.Properties)
                .Select(property => false).ToList();
        }
        else
        {
            enabled = _categories.SelectMany(category => category.Properties)
                .Select(property => property.PowerCost <= remainingBudget).ToList();
        }

        _token.SetBindValues(PropertyEnabled, enabled);
    }

    private void UpdateItemName()
    {
        _token.SetBindValue(ItemName, _selection.Name);
    }

    private void UpdatePropertyList()
    {
        List<string> selectionPropertyNames = new();
        foreach (ItemProperty property in _existingProperties)
        {
            if (_removedProperties.Contains(property)) continue;

            selectionPropertyNames.Add(ItemPropertyHelper.GameLabel(property));
        }

        _token.SetBindValues(ItemPropertyNames, selectionPropertyNames);

        int count = selectionPropertyNames.Count;
        _token.SetBindValue(ItemPropertyCount, count);

        List<string> selectionPropertyPowers = _selection.ItemProperties
            .Select(ItemPropertyHelper.ToCraftingProperty)
            .Select(model => model.PowerCost.ToString()).ToList();
        _token.SetBindValues(ItemPropertyPowers, selectionPropertyPowers);

        UpdateRemovables();
    }

    private void UpdateRemovables()
    {
        List<bool> removables = _existingProperties.Select(ItemPropertyHelper.CanBeRemoved)
            .ToList();

        _token.SetBindValues(Removables, removables);
    }


    private void OnNuiEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            // Button events
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        HandleChangeListAddition(eventData);
        HandleChangeListRemoval(eventData);
        HandleItemPropertyRemoval(eventData);
    }

    private void HandleItemPropertyRemoval(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId != RemoveItemProperty) return;

        int index = eventData.ArrayIndex;
        _token.Player.SendServerMessage("Removing property at index: " + index);
        _token.Player.SendServerMessage($"{_existingProperties.Count} existing properties");
        ItemProperty property = _existingProperties[index];

        _removedProperties.Add(property);
        _existingProperties.Remove(property);

        ChangelistEntry entry = new()
        {
            Label = ItemPropertyHelper.GameLabel(property),
            Property = ItemPropertyHelper.ToCraftingProperty(property),
            GpCost = 0,
            State = ChangeState.Removed
        };

        _changeList.Add(entry);

        UpdatePropertyList();
        UpdateChangeListView();
    }

    private void HandleChangeListAddition(ModuleEvents.OnNuiEvent eventData)
    {
        bool exists = _itemProperties.TryGetValue(eventData.ElementId, out CraftingProperty? p);
        if (p == null) return;
        if (!exists) return;

        ValidateCost(p);

        AddToChangeList(p);
        CheckForExisting(p);
        UpdateChangeListView();

        UpdateSelectableProperties();
    }

    private void ValidateCost(CraftingProperty property)
    {
    }

    private void AddToChangeList(CraftingProperty property)
    {
        ChangelistEntry entry = new()
        {
            Label = property.GuiLabel,
            Property = property,
            GpCost = property.CalculateGoldCost(),
            State = ChangeState.Added,
        };
        _changeList.Add(entry);
    }


    private void CheckForExisting(CraftingProperty property)
    {
        int baseItem = NWScript.GetBaseItemType(_selection);
        CraftingProperty? existingProperty = _data.UncategorizedPropertiesFor(baseItem)
            .FirstOrDefault(g => g.GameLabel == property.GameLabel);

        string existingLabel = existingProperty?.GuiLabel ?? ItemPropertyHelper.GameLabel(property.ItemProperty);

        existingProperty ??= ItemPropertyHelper.ToCraftingProperty(property.ItemProperty);

        // Check for existing properties of the same type and flag them for removal
        int propType = NWScript.GetItemPropertyType(property.ItemProperty);
        if (NWScript.GetItemHasItemProperty(_selection, propType) == NWScript.TRUE)
        {
            ChangelistEntry removal = new ChangelistEntry
            {
                Label = existingLabel,
                Property = existingProperty,
                GpCost = 0,
                State = ChangeState.Replaced,
            };

            _changeList.Add(removal);
        }
    }

    private int CalculateGoldCost(CraftingProperty property)
    {
        return 0;
    }

    private void UpdateChangeListView()
    {
        List<string> labels = _changeList.Select(entry => entry.Label).ToList();
        _token.SetBindValues(EntryLabels, labels);

        List<string> powers = _changeList.Select(entry => entry.Property.PowerCost.ToString()).ToList();
        _token.SetBindValues(EntryPowerCosts, powers);

        _token.SetBindValue(EntryCount, _changeList.Count);

        List<Color> colors = _changeList.Select(entry => ColorForState(entry.State)).ToList();
        _token.SetBindValues(ItemPropertyColors, colors);
    }

    private static Color ColorForState(ChangeState state)
    {
        return state switch
        {
            ChangeState.Added => ColorConstants.Lime,
            ChangeState.Removed => ColorConstants.Red,
            ChangeState.Replaced => ColorConstants.Yellow,
            _ => ColorConstants.White
        };
    }

    private void RemoveProperties()
    {
        foreach (ChangelistEntry entry in _changeList.Where(itemProperty => itemProperty.State == ChangeState.Removed))
        {
            _selection.RemoveItemProperty(entry.Property.ItemProperty);
        }
    }

    private void AddProperties()
    {
        foreach (ChangelistEntry entry in _changeList.Where(itemProperty => itemProperty.State == ChangeState.Added))
        {
            _selection.AddItemProperty(entry.Property.ItemProperty, EffectDuration.Permanent);
        }
    }

    private void HandleChangeListRemoval(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click || eventData.ElementId != RemoveChangeId)
        {
            return;
        }

        int index = eventData.ArrayIndex;

        CraftingProperty property = _changeList[index].Property;
        property.Removeable = true;
        _changeList.RemoveAt(index);

        _existingProperties.Add(property.ItemProperty);
        _removedProperties.Remove(property.ItemProperty);

        UpdateChangeListView();
        UpdatePropertyList();
        UpdateRemovables();
    }

    public void Close()
    {
        _token.Dispose();
    }

    // Private classes...Used for internal tracking of changes
    private class ChangelistEntry
    {
        public required string Label { get; set; }
        public required CraftingProperty Property { get; set; }
        public int GpCost { get; set; }
        public ChangeState State { get; set; }
    }

    private enum ChangeState
    {
        Added,
        Removed,
        Replaced
    }
}