using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.NwObjectHelpers;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class CraftingWindow
{
    private List<PropertyListEntry> _existing = new();
    private List<PropertyListEntry> _changelist = new();
    private NuiWindow? WindowTemplate { get; set; }

    private NwItem _selection;
    private readonly CraftingPropertyData _data;
    private readonly CraftingBudgetService _budget;
    private readonly NwPlayer _player;
    private NuiWindowToken _token;

    private readonly List<NuiColumn> _combos = new();

    private readonly NuiBind<string> _itemPropertyNames = new("item_properties");
    private readonly NuiBind<string> _itemPropertyCosts = new("item_property_costs");
    private readonly NuiBind<int> _itemPropertyCount = new("item_property_count");
    private readonly NuiBind<bool> _propertyCanBeRemoved = new("removeable");

    private readonly NuiBind<string> _changingItemPropertyNames = new("changelist_properties");
    private readonly NuiBind<string> _changingItemPropertyCosts = new("changelist_costs");
    private readonly NuiBind<int> _changingItemPropertyCount = new("changelist_count");

    private readonly IReadOnlyList<CraftingCategory> _categories;
    private readonly List<NuiButtonImage> _addButtons = new();

    private readonly NuiBind<string> _maxBudget = new("max_budget");
    private readonly NuiBind<string> _spent = new("spent");
    private int _maxNum;
    private int _spentNum;

    public CraftingWindow(NwPlayer player, NwItem selection, CraftingPropertyData data, CraftingBudgetService budget)
    {
        _selection = selection;
        _data = data;
        _budget = budget;
        _player = player;

        int baseItemType = NWScript.GetBaseItemType(_selection);
        _categories = _data.Properties[baseItemType];

        WindowTemplate = BuildWindow();

        NwModule.Instance.OnNuiEvent += OnNuiEvent;
    }

    private void OnNuiEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;
        if (obj.ElementId is null) return;
        if (obj.ElementId.StartsWith("ip_remove"))
        {
        }

        if (_addButtons.Any(b => b.Id == obj.ElementId))
        {
        }

        if (_categories.Any(c => c.Properties.Any(p => p.Button.Id == obj.ElementId)))
        {
        }
    }

    private NuiWindow? BuildWindow()
    {
        List<NuiElement> entries = new();
        foreach (CraftingCategory category in _categories)
        {
            NuiColumn nuiElement = category.ToColumnWithGroup();
            _combos.Add(nuiElement);

            NuiRow element = new()
            {
                Children =
                {
                    nuiElement
                }
            };

            entries.Add(element);
        }

        List<NuiListTemplateCell> existingItemProperties = new()
        {
            new NuiListTemplateCell(new NuiLabel(_itemPropertyNames)),
            new NuiListTemplateCell(new NuiLabel(_itemPropertyCosts)),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = "ip_remove",
                Enabled = _propertyCanBeRemoved
            })
        };

        List<NuiListTemplateCell> proposedChanges = new()
        {
            new NuiListTemplateCell(new NuiLabel(_changingItemPropertyNames)),
            new NuiListTemplateCell(new NuiLabel(_changingItemPropertyCosts)),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = "changelist_remove",
                Tooltip = "Undo this change.",
            })
        };
        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel($"Editing {_selection.Name}")
                        {
                            Margin = 10f,
                            Padding = 10f
                        },
                        new NuiLabel("Budget:")
                        {
                            Margin = 10f,
                            Padding = 10f
                        },
                        new NuiGroup()
                        {
                            Element = new NuiLabel(_maxBudget)
                            {
                                Margin = 10f,
                                Padding = 10f
                            },
                            Border = true
                        },
                        new NuiLabel("Spent:")
                        {
                            Margin = 10f,
                            Padding = 10f
                        },
                        new NuiGroup()
                        {
                            Element = new NuiLabel(_spent)
                            {
                                Margin = 10f,
                                Padding = 10f
                            },
                            Border = true
                        },
                    }
                },
                new NuiSpacer(),
                new NuiRow
                {
                    Children =
                    {
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiGroup
                                {
                                    Element = new NuiColumn
                                    {
                                        Children = entries,
                                        Height = 400f,
                                        Width = 400f
                                    }
                                }
                            }
                        },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiLabel("Existing Properties | Total Cost")
                                {
                                    HorizontalAlign = NuiHAlign.Center,
                                    Padding = 60f
                                },
                                new NuiRow
                                {
                                    Children =
                                    {
                                        new NuiList(existingItemProperties, _itemPropertyCount)
                                        {
                                            RowHeight = 35f,
                                            Width = 400f,
                                            Height = 400f
                                        },
                                        new NuiList(proposedChanges, _changingItemPropertyCount)
                                        {
                                            RowHeight = 35f,
                                            Width = 400f,
                                            Height = 400f
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("Apply Changes")
                        {
                            Id = "apply_changes",
                            Width = 200f,
                            Height = 50f
                        }
                    }
                }
            }
        };


        return new NuiWindow(root, "Mythal Forge")
        {
            Geometry = new NuiRect(400, 400, 800f, 800f)
        };
    }

    private void PopulateItemProperties()
    {
        List<string> labels = new();
        List<string> costs = new();
        List<bool> removeables = new();
        int propertyCount = _selection.ItemProperties.Count();

        foreach (ItemProperty property in _selection.ItemProperties)
        {
            string gameLabel = ItemPropertyHelper.GameLabel(property);
            string label = _categories.SelectMany(c => c.Properties).FirstOrDefault(p => p.GameLabel == gameLabel)
                ?.GuiLabel ?? gameLabel;
            string cost = _categories.SelectMany(c => c.Properties).FirstOrDefault(p => p.GameLabel == gameLabel)
                ?.PowerCost
                .ToString() ?? "2";

            bool removable = _categories.SelectMany(c => c.Properties).FirstOrDefault(p => p.GameLabel == gameLabel)
                ?.Removeable ?? false;

            labels.Add(label);
            costs.Add(cost);
            removeables.Add(removable);

            PropertyListEntry entry = new()
            {
                Label = label,
                State = EntryState.Added,
                CraftingModel =
                    _categories.SelectMany(c => c.Properties).FirstOrDefault(p => p.GameLabel == gameLabel) ??
                    ModelFromProperty(property)
            };

            _existing.Add(entry);
        }

        _token.SetBindValues(_itemPropertyNames, labels);
        _token.SetBindValues(_itemPropertyCosts, costs);
        _token.SetBindValues(_propertyCanBeRemoved, removeables);

        _token.SetBindValue(_itemPropertyCount, propertyCount);
    }

    private CraftingProperty ModelFromProperty(ItemProperty property)
    {
        string gameLabel = ItemPropertyHelper.GameLabel(property);
        return new CraftingProperty
        {
            ItemProperty = property,
            GuiLabel = gameLabel,
            PowerCost = 2,
            CraftingTier = CraftingTier.DreamCoin
        };
    }

    private void UpdateChangelist()
    {
        List<string> changelistPropertyNames = new();
        List<string> changelistCosts = new();

        foreach (PropertyListEntry propertyListEntry in _changelist)
        {
            changelistPropertyNames.Add(propertyListEntry.CraftingModel.GuiLabel);
            changelistCosts.Add(propertyListEntry.CraftingModel.PowerCost.ToString());
        }

        int changelistCount = _changelist.Count;

        _token.SetBindValues(_changingItemPropertyNames, changelistPropertyNames);
        _token.SetBindValues(_changingItemPropertyCosts, changelistCosts);
        _token.SetBindValue(_changingItemPropertyCount, changelistCount);
    }

    public void OpenWindow()
    {
        if (WindowTemplate is null) return;
        _player.TryCreateNuiWindow(this.WindowTemplate, out _token);
        PopulateItemProperties();
        foreach (CraftingCategory craftingPropertyCategory in _categories)
        {
            craftingPropertyCategory.UpdateComboSelection(_token, 0);
        }

        int baseItemType = NWScript.GetBaseItemType(_selection);
        _maxNum = _budget.MythalBudgetFor(baseItemType);
        _spentNum = _selection.ItemProperties.Sum(p => _categories.SelectMany(c => c.Properties)
            .FirstOrDefault(cp => cp.GameLabel == ItemPropertyHelper.GameLabel(p))?.PowerCost ?? 2);

        _token.SetBindValue(_maxBudget, _maxNum.ToString());
        _token.SetBindValue(_spent, _spentNum.ToString());
    }

    public void CloseWindow()
    {
        _token.Dispose();
    }


    private void RecalculateBudget()
    {
        _spentNum = _selection.ItemProperties.Sum(p => _categories.SelectMany(c => c.Properties)
                        .FirstOrDefault(cp => cp.GameLabel == ItemPropertyHelper.GameLabel(p))?.PowerCost ?? 2) +
                    _changelist.Sum(p => p.CraftingModel.PowerCost);

        _token.SetBindValue(_spent, _spentNum.ToString());
    }

    private sealed class PropertyListEntry
    {
        /// <summary>
        /// The GUI label for this item property.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The state of this entry.
        /// </summary>
        public EntryState State { get; set; }

        /// <summary>
        /// Internal reference to the crafting model for this item property.
        /// </summary>
        public CraftingProperty CraftingModel { get; set; }
    }

    private enum EntryState
    {
        Added,
        Removed,
        Changed,
        Unchanged
    }
}