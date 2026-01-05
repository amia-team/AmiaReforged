using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.DmForge;

public sealed class DmForgeView : ScryView<DmForgePresenter>
{
    public const string ApplyNameButtonId = "dm_apply_name";
    public const string CloseId = "dm_close";

    public DmForgeView(DmForgePresenter presenter)
    {
        Presenter = presenter;
    }

    public override DmForgePresenter Presenter { get; protected set; }

    // Binds
    public NuiBind<string> ItemName { get; } = new("dm_item_name");

    // Current properties list
    public NuiBind<int> CurrentCount { get; } = new("dm_current_count");
    public NuiBind<string> CurrentLabels { get; } = new("dm_current_labels");
    public NuiBind<bool> CurrentRemovable { get; } = new("dm_current_removable");
    public string CurrentRemoveId => "dm_current_remove";

    // Available properties list
    public NuiBind<int> AvailableCount { get; } = new("dm_avail_count");
    public NuiBind<string> AvailableLabels { get; } = new("dm_avail_labels");
    public string AvailableAddId => "dm_avail_add";

    // Search
    public NuiBind<string> SearchBind { get; } = new("dm_search");

    public NuiBind<string> PowerTotal { get; } = new("power_total");

    /// <summary>
    /// Gets the root layout for the DmForgeView.
    /// This defines the main layout structure for the view in the UI system.
    /// </summary>
    /// <returns>
    /// A NuiLayout object representing the root layout of the DmForgeView.
    /// </returns>
    public override NuiLayout RootLayout()
    {
        // Current props list
        List<NuiListTemplateCell> currentCells = new List<NuiListTemplateCell>
        {
            new(new NuiLabel(CurrentLabels)),
            new(new NuiButtonImage("ir_abort")
            {
                Id = CurrentRemoveId,
                Enabled = CurrentRemovable
            })
            {
                Width = 30f,
                VariableSize = false
            }
        };

        // Available props list
        List<NuiListTemplateCell> availableCells = new List<NuiListTemplateCell>
        {
            new(new NuiLabel(AvailableLabels)),
            new(new NuiButtonImage("ir_craft") // same add icon style as mythal forge
            {
                Id = AvailableAddId
            })
            {
                Width = 30f,
                VariableSize = false
            }
        };

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 1100f, 600f))]
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit("Edit Name", ItemName, 100, false) { Width = 250f, Height = 40f },
                        new NuiButton("Change Name") { Id = ApplyNameButtonId, Height = 40f, Width = 140f },
                        new NuiSpacer(),
                        new NuiGroup()
                        {
                            Width = 60f,
                            Height = 60f,
                            Element = new NuiLabel(PowerTotal)
                            {
                                VerticalAlign = NuiVAlign.Middle,
                                HorizontalAlign = NuiHAlign.Center
                            }
                        },
                        new NuiButton("Close")
                        {
                            Id = CloseId, Height = 40f, Width = 120f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiGroup
                        {
                            Element = new NuiColumn
                            {
                                Children =
                                {
                                    new NuiLabel("Current Properties")
                                    {
                                        Height = 15f
                                    },
                                    new NuiList(currentCells, CurrentCount) { RowHeight = 28f }
                                }
                            },
                            Width = 520f,
                            Height = 500f,
                            Border = true
                        },
                        new NuiGroup
                        {
                            Element = new NuiColumn
                            {
                                Children =
                                {
                                    new NuiRow
                                    {
                                        Height = 30f,
                                        Children =
                                        {
                                            new NuiLabel("Search:"),
                                            new NuiTextEdit("type to filter...", SearchBind, 64, false)
                                            {
                                                Width = 260f
                                            }
                                        }
                                    },
                                    new NuiLabel("Available Properties")
                                    {
                                        Height = 15f
                                    },
                                    new NuiList(availableCells, AvailableCount) { RowHeight = 28f }
                                }
                            },
                            Width = 520f,
                            Height = 500f,
                            Border = true
                        }
                    }
                }
            }
        };
    }
}
