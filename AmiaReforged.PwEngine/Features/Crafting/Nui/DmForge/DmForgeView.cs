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
            new(new NuiButtonImage("ui_btn_forgerem")
            {
                Id = CurrentRemoveId,
                Enabled = CurrentRemovable
            })
            {
                Width = 25f,
                VariableSize = false
            }
        };

        // Available props list
        List<NuiListTemplateCell> availableCells = new List<NuiListTemplateCell>
        {
            new(new NuiLabel(AvailableLabels)),
            new(new NuiButtonImage("ui_btn_forgeadd")
            {
                Id = AvailableAddId
            })
            {
                Width = 25f,
                VariableSize = false
            }
        };

        return new NuiColumn
        {
            Children =
            {
                // Background image matching Mythal Forge style
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_forge_dm", new NuiRect(-30f, -15f, 1220f, 813f))]
                },
                new NuiSpacer { Height = 180f },

                new NuiRow
                {
                    Children =
                    {
                        // Name editing row with styled buttons
                        new NuiTextEdit("Edit Name", ItemName, 100, false)
                        {
                            Width = 140f,
                            Height = 38f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButtonImage("ui_btn_rename")
                        {
                            Id = ApplyNameButtonId,
                            Tooltip = "Change this item's name",
                            Width = 150f,
                            Height = 38f
                        },
                        new NuiSpacer { Width = 114f },
                        new NuiTextEdit("Filter...", SearchBind, 64, false)
                        {
                            Tooltip = "Filter properties by name",
                            Width = 200f,
                            Height = 38f
                        }
                    }
                },
                // Main content: two property lists side by side
                new NuiRow
                {
                    Children =
                    {
                        // Current Properties column
                        new NuiColumn
                        {
                            Width = 300f,
                            Children =
                            {
                                new NuiRow
                                {
                                    Children =
                                    {
                                        new NuiLabel("Item Power Total:")
                                        {
                                            Height = 25f,
                                            Width = 140f,
                                            VerticalAlign = NuiVAlign.Middle
                                        },
                                        new NuiSpacer { Width = 10f },
                                        new NuiLabel(PowerTotal)
                                        {
                                            Width = 20f,
                                            Height = 25f,
                                            VerticalAlign = NuiVAlign.Middle,
                                            HorizontalAlign = NuiHAlign.Center,
                                            Tooltip = "Total power on this item"
                                        }
                                    }
                                },
                                new NuiList(currentCells, CurrentCount)
                                {
                                    RowHeight = 25f,
                                    Height = 250f
                                }
                            }
                        },
                        new NuiSpacer { Width = 20f },
                        // Available Properties column
                        new NuiColumn
                        {
                            Width = 300f,
                            Children =
                            {
                                new NuiSpacer { Height = 4f },
                                new NuiLabel("Available Properties")
                                {
                                    Height = 25f,
                                    HorizontalAlign = NuiHAlign.Center
                                },
                                new NuiList(availableCells, AvailableCount)
                                {
                                    RowHeight = 25f,
                                    Height = 250f
                                }
                            }
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                // Bottom button row
                new NuiRow
                {
                    Children =
                    {
                        new NuiButtonImage("ui_btn_cancelf")
                        {
                            Id = CloseId,
                            Tooltip = "Close the DM Forge",
                            Width = 150f,
                            Height = 38f
                        }
                    }
                }
            }
        };
    }
}
