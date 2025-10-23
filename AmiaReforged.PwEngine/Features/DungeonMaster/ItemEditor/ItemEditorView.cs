using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.ItemEditor;

public sealed class ItemEditorView : ScryView<ItemEditorPresenter>, IDmWindow
{
    public override ItemEditorPresenter Presenter { get; protected set; }

    // Basic item properties
    public readonly NuiBind<string> Name = new("item_name");
    public readonly NuiBind<string> Description = new("item_desc");
    public readonly NuiBind<string> Tag = new("item_tag");
    public readonly NuiBind<bool> ValidObjectSelected = new("valid_obj");

    // Icon/edit binds
    public readonly NuiBind<bool> IconControlsVisible = new("ie_icon_visible");
    public readonly NuiBind<string> IconInfo = new("ie_icon_info");

    // Variables
    public readonly NuiBind<int> VariableCount = new("var_count");
    public readonly NuiBind<string> VariableNames = new("var_names");
    public readonly NuiBind<string> VariableValues = new("var_values");
    public readonly NuiBind<string> VariableTypes = new("var_types");

    // New variable inputs
    public readonly NuiBind<string> NewVariableName = new("new_var_name");
    public readonly NuiBind<int> NewVariableType = new("new_var_type");
    public readonly NuiBind<string> NewVariableValue = new("new_var_value");

    // Buttons
    public NuiButton SelectItemButton = null!;
    public NuiButton SaveButton = null!;
    public NuiButton AddVariableButton = null!;
    public NuiButtonImage DeleteVariableButton = null!;

    // Icon buttons
    public NuiButton IconPlus1 = null!;
    public NuiButton IconMinus1 = null!;
    public NuiButton IconPlus10 = null!;
    public NuiButton IconMinus10 = null!;

    public string Title => "Item Editor";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public ItemEditorView(NwPlayer player)
    {
        Presenter = new ItemEditorPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // Variable list template
        List<NuiListTemplateCell> variableCells =
        [
            new(new NuiLabel(VariableNames)
            {
                VerticalAlign = NuiVAlign.Middle,
                Width = 150f
            })
            {
                Width = 150f
            },

            new(new NuiLabel(VariableTypes)
            {
                VerticalAlign = NuiVAlign.Middle,
                Width = 80f
            })
            {
                Width = 80f
            },

            new(new NuiLabel(VariableValues)
            {
                VerticalAlign = NuiVAlign.Middle
            }),

            new(new NuiButtonImage("ir_abort")
            {
                Id = "btn_delete_var",
                Tooltip = "Delete Variable"
            }.Assign(out DeleteVariableButton))
            {
                Width = 35f,
                VariableSize = false
            }
        ];

        // Icon control row (constraint-safe)
        NuiRow iconRow = new()
        {
            Children =
            [
                new NuiLabel(IconInfo) { VerticalAlign = NuiVAlign.Middle },
                new NuiSpacer(),
                new NuiButton("+1")
                {
                    Id = "ie_icon_p1",
                    Enabled = IconControlsVisible,
                    Width = 50f
                }.Assign(out IconPlus1),
                new NuiButton("-1")
                {
                    Id = "ie_icon_m1",
                    Enabled = IconControlsVisible,
                    Width = 50f
                }.Assign(out IconMinus1),
                new NuiButton("+10")
                {
                    Id = "ie_icon_p10",
                    Enabled = IconControlsVisible,
                    Width = 60f
                }.Assign(out IconPlus10),
                new NuiButton("-10")
                {
                    Id = "ie_icon_m10",
                    Enabled = IconControlsVisible,
                    Width = 60f
                }.Assign(out IconMinus10),
            ]
        };

        return new NuiColumn
        {
            Width = 700f,
            Children =
            {
                new NuiRow()
                {
                    Children =
                    [
                        new NuiButton("Select Item")
                        {
                            Id = "btn_select_item",
                            Height = 35f
                        }.Assign(out SelectItemButton),
                    ]
                },

                new NuiSpacer { Height = 10f },

                new NuiRow()
                {
                    Width = 400f,
                    Height = 220f,
                    Children =
                    [
                        new NuiGroup
                        {
                            Border = true,
                            Element = new NuiColumn
                            {
                                Children =
                                {
                                    new NuiLabel("Basic Properties")
                                    {
                                        Height = 20f,
                                        HorizontalAlign = NuiHAlign.Center
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        {
                                            new NuiLabel("Name:")
                                            {
                                                Width = 80f,
                                                VerticalAlign = NuiVAlign.Middle
                                            },
                                            new NuiTextEdit("Item Name", Name, 100, false)
                                            {
                                                Enabled = ValidObjectSelected
                                            }
                                        }
                                    },
                                    new NuiRow
                                    {
                                        Children =
                                        {
                                            new NuiLabel("Tag:")
                                            {
                                                Width = 80f,
                                                VerticalAlign = NuiVAlign.Middle
                                            },
                                            new NuiTextEdit("Item Tag", Tag, 32, false)
                                            {
                                                Enabled = ValidObjectSelected
                                            }
                                        }
                                    },
                                    new NuiLabel("Description:")
                                    {
                                        Height = 20f,
                                        VerticalAlign = NuiVAlign.Middle
                                    },
                                    new NuiTextEdit("Item Description", Description, 5000, true)
                                    {
                                        Height = 100f,
                                        Enabled = ValidObjectSelected
                                    }
                                }
                            }
                        },
                    ]
                },

                new NuiSpacer { Height = 8f },

                // Icon controls (always visible; buttons disabled when not allowed)
                new NuiGroup
                {
                    Border = true,
                    Element = new NuiColumn
                    {
                        Children =
                        {
                            new NuiLabel("Icon / Simple Model")
                            {
                                Height = 20f,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            iconRow
                        }
                    }
                },

                new NuiSpacer { Height = 10f },

                new NuiRow
                {
                    Width = 700f,
                    Height = 400f,
                    Children =
                    [
                        new NuiGroup
                        {
                            Border = true,
                            Element = new NuiColumn
                            {
                                Children =
                                {
                                    new NuiLabel("Local Variables")
                                    {
                                        Height = 20f,
                                        HorizontalAlign = NuiHAlign.Center
                                    },

                                    // Add new variable section
                                    new NuiRow
                                    {
                                        Children =
                                        {
                                            new NuiTextEdit("Variable Name", NewVariableName, 32, false)
                                            {
                                                Width = 150f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiCombo
                                            {
                                                Entries = new NuiValue<List<NuiComboEntry>>(new List<NuiComboEntry>
                                                {
                                                    new("Int", 0),
                                                    new("Float", 1),
                                                    new("String", 2)
                                                }),
                                                Selected = NewVariableType,
                                                Width = 100f,
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiTextEdit("Value", NewVariableValue, 100, false)
                                            {
                                                Enabled = ValidObjectSelected
                                            },
                                            new NuiButton("Add")
                                            {
                                                Id = "btn_add_var",
                                                Width = 60f,
                                                Enabled = ValidObjectSelected
                                            }.Assign(out AddVariableButton)
                                        }
                                    },

                                    new NuiSpacer { Height = 5f },

                                    // Variables list
                                    new NuiList(variableCells, VariableCount)
                                    {
                                        RowHeight = 30f,
                                        Height = 250f
                                    }
                                }
                            }
                        },
                    ]
                },

                new NuiSpacer { Height = 10f },

                // Save button
                new NuiButton("Save Changes")
                {
                    Id = "btn_save",
                    Height = 35f,
                    Enabled = ValidObjectSelected
                }.Assign(out SaveButton)
            }
        };
    }
}
