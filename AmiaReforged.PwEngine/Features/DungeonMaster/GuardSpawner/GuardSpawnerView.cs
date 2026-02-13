using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.GuardSpawner;

/// <summary>
/// View for the Guard Spawner DM tool. Defines the NUI layout and bindings.
/// </summary>
public sealed class GuardSpawnerView : ScryView<GuardSpawnerPresenter>, IDmWindow
{
    // Window dimensions
    private const float WindowWidth = 500f;
    private const float WindowHeight = 500f;

    // Standard label color for parchment background
    private static readonly Color LabelColor = new(30, 20, 12);

    // Binds for dropdowns
    public readonly NuiBind<List<NuiComboEntry>> SettlementEntries = new("settlement_entries");
    public readonly NuiBind<int> SelectedSettlementIndex = new("selected_settlement");
    public readonly NuiBind<List<NuiComboEntry>> CreatureEntries = new("creature_entries");
    public readonly NuiBind<int> SelectedCreatureIndex = new("selected_creature");

    // Binds for guard list
    public readonly NuiBind<int> GuardCount = new("guard_count");
    public readonly NuiBind<string> GuardNames = new("guard_names");

    // Binds for inputs
    public readonly NuiBind<string> QuantityText = new("quantity_text");
    public readonly NuiBind<string> WidgetNameText = new("widget_name_text");

    // Binds for beacon button
    public readonly NuiBind<string> BeaconButtonLabel = new("beacon_label");
    public readonly NuiBind<string> BeaconTooltip = new("beacon_tooltip");

    // Button references
    public NuiButtonImage SelectWidgetButton = null!;
    public NuiButtonImage AddCreatureButton = null!;
    public NuiButtonImage RemoveCreatureButton = null!;
    public NuiButton BeaconButton = null!;
    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage ResetButton = null!;

    public override GuardSpawnerPresenter Presenter { get; protected set; }

    public string Title => "Guard Spawner";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public GuardSpawnerView(NwPlayer player)
    {
        Presenter = new GuardSpawnerPresenter(this, player);
        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    /// <summary>
    /// Helper method to create an image button with optional label below.
    /// </summary>
    private static NuiElement ImagePlatedLabeledButton(string id, string tooltip, out NuiButtonImage logicalButton,
        string plateResRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return btn;
    }

    public override NuiLayout RootLayout()
    {
        // Background parchment layer
        NuiRow bgLayer = new()
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowWidth, WindowHeight))]
        };

        // Guard list template
        List<NuiListTemplateCell> guardListCells =
        [
            new(new NuiLabel(GuardNames)
            {
                VerticalAlign = NuiVAlign.Middle
            }),
            new(new NuiButtonImage("ui_btn_sm_x")
            {
                Id = "btn_remove_guard",
                Width = 25f,
                Height = 25f,
                Tooltip = "Remove this guard"
            }.Assign(out RemoveCreatureButton))
            {
                VariableSize = false,
                Width = 35f
            }
        ];

        NuiColumn rootLayout = new()
        {
            Children =
            [
                // Background layer
                bgLayer,

                // Row 1: Select existing widget button
                new NuiRow
                {
                    Children =
                    [
                        new NuiButtonImage("nui_pick")
                        {
                            Id = "btn_select_widget",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Select an existing guard widget to edit"
                        }.Assign(out SelectWidgetButton),
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Select Existing Widget (Optional)")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        }
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Row 2: Settlement dropdown
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("Settlement:")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        },
                        new NuiCombo
                        {
                            Id = "combo_settlement",
                            Entries = SettlementEntries,
                            Selected = SelectedSettlementIndex,
                            Width = 300f
                        }
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 5f },

                // Row 3: Creature dropdown + Add button
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("Creature:")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        },
                        new NuiCombo
                        {
                            Id = "combo_creature",
                            Entries = CreatureEntries,
                            Selected = SelectedCreatureIndex,
                            Width = 250f
                        },
                        new NuiButtonImage("ui_btn_sm_plus1")
                        {
                            Id = "btn_add_creature",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "Add this creature to the widget (max 4)"
                        }.Assign(out AddCreatureButton)
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Row 4: Guard list header
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("Selected Guards (max 4):")
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        }
                    ]
                },

                // Guard list (not wrapped in a row)
                new NuiList(guardListCells, GuardCount)
                {
                    RowHeight = 35f,
                    Width = WindowWidth - 30f,
                    Height = 150f
                },

                // Spacer
                new NuiSpacer { Height = 10f },

                // Row 5: Quantity and Name inputs
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("Qty (1-8):")
                        {
                            Width = 65f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        },
                        new NuiTextEdit("1", QuantityText, 2, false)
                        {
                            Width = 50f,
                            Height = 35f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Name:")
                        {
                            Width = 50f,
                            Tooltip = "Template: Summon {settlement/Nation} {Input Name}",
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = LabelColor
                        },
                        new NuiTextEdit("Widget Name", WidgetNameText, 50, false)
                        {
                            Width = 200f,
                            Height = 35f
                        }
                    ]
                },

                // Spacer
                new NuiSpacer { Height = 15f },

                // Row 6: Beacon toggle, Save, Reset buttons
                new NuiRow
                {
                    Children =
                    [
                        new NuiButton(BeaconButtonLabel)
                        {
                            Id = "btn_beacon",
                            Width = 120f,
                            Height = 35f,
                            Tooltip = BeaconTooltip
                        }.Assign(out BeaconButton),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_save", "Create or update the guard widget", out SaveButton, "ui_btn_save"),
                        new NuiSpacer { Width = 10f },
                        ImagePlatedLabeledButton("btn_reset", "Clear all selections and start over", out ResetButton, "ui_btn_discard")
                    ]
                }
            ]
        };

        return rootLayout;
    }

    /// <summary>
    /// Gets the window width for presenter use.
    /// </summary>
    public static float GetWindowWidth() => WindowWidth;

    /// <summary>
    /// Gets the window height for presenter use.
    /// </summary>
    public static float GetWindowHeight() => WindowHeight;
}


