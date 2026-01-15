using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
///     Represents the help guide view for the Mythal Forge crafting system.
/// </summary>
public sealed class MythalForgeHelpView : ScryView<MythalForgeHelpPresenter>
{
    public MythalForgeHelpView(NwPlayer player)
    {
        Presenter = new MythalForgeHelpPresenter(player, this);
    }

    public override MythalForgeHelpPresenter Presenter { get; protected set; }

    /// <summary>
    ///     Defines the root layout of the Mythal Forge help guide.
    /// </summary>
    /// <returns>The root layout of the view.</returns>
    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_forge_h", new NuiRect(-200f, -10f, 1200f, 800f))]
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel("FORGE GUIDE")
                        {
                            Width = 600f,
                            Height = 40f,
                            ForegroundColor = new Color(205, 202, 71),
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                CreateSectionHeader("OVERVIEW"),
                CreateText("The Forge allows you to customize the properties on your items."),
                CreateText("Each item has a power cost budget that limits what you can apply."),
                new NuiSpacer { Height = 30f },

                CreateSectionHeader("MAIN DISPLAY"),
                new NuiSpacer { Height = 5f },
                CreateSubHeader("Property Browser (Left Column)"),
                CreateText("- Filter: Type text to search for specific properties"),
                CreateText("- Category: Select a category from the dropdown menu"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_btn_forgesrch")
                        {
                            Width = 38f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Search Button")
                        {
                            Width = 200f,
                            Height = 38f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                CreateText("- Search Button: You must click this button to apply your filters"),
                CreateText("- Property List: Shows all available properties you can add"),
                CreateText("- Click the + button next to a property to add it to your item"),
                CreateSubHeader("Active Properties (Right Column)"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_forge_cw")
                        {
                            Width = 35f,
                            Height = 35f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Caster Weapon Icon")
                        {
                            Width = 200f,
                            Height = 35f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                CreateText("- Caster Weapon Icon: indicates this is a Caster Weapon, when visible"),
                CreateText("- Name Field: Edit the item's name using the text field"),
                CreateText("- Max Powers: The maximum power budget for this item"),
                CreateText("- Remaining Powers: How much budget you have left"),
                CreateText("- Properties Column: Shows all properties currently applied to your item"),
                CreateText("- Click the X button to remove a property"),
                new NuiSpacer { Height = 30f },

                CreateSectionHeader("ACTION BUTTONS"),
                new NuiSpacer { Height = 5f },
                CreateSubHeader("Caster Weapon Button"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_btn_castweap")
                        {
                            Width = 150f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiLabel("Caster Weapon Button")
                        {
                            Width = 200f,
                            Height = 38f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                CreateText("- Converts the weapon to/from a Caster Type Weapon"),
                CreateText("- 1H caster weapons have 8 power cost allotment"),
                CreateText("- 2H caster weapons have 16 power cost allotment"),
                new NuiSpacer { Height = 5f },

                CreateSubHeader("Rename Button"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_btn_rename")
                        {
                            Width = 150f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiLabel("Rename Button")
                        {
                            Width = 200f,
                            Height = 38f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                CreateText("- Applies the name you've entered in the text field"),
                new NuiSpacer { Height = 5f },

                CreateSubHeader("Cancel Button"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_btn_cancelf")
                        {
                            Width = 150f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiLabel("Cancel Button")
                        {
                            Width = 200f,
                            Height = 38f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                CreateText("- Closes the forge without applying any changes"),
                new NuiSpacer { Height = 5f },

                CreateSubHeader("Forge Button"),
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 40f },
                        new NuiImage("ui_btn_forge")
                        {
                            Width = 150f,
                            Height = 38f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiSpacer { Width = 5f },
                        new NuiLabel("Forge Button")
                        {
                            Width = 200f,
                            Height = 38f,
                            ForegroundColor = new Color(205, 202, 71),
                            VerticalAlign = NuiVAlign.Middle
                        }
                    }
                },
                CreateText("- Attempts to apply all changes to your item"),
                CreateText("- Requires sufficient gold and a skill check"),
                CreateText("- Difficulty: Shows the DC you must beat"),
                CreateText("- Gold Cost: Amount deducted from your inventory"),
                new NuiSpacer { Height = 30f },

                CreateSectionHeader("TIPS"),

                CreateText("- Plan your equipment carefully. Budgets are limited!"),
                CreateText("- Some properties have prerequisites or restrictions."),
                CreateText("- Red text in the Difficulty field means you can't beat the DC."),
                CreateText("- Red text in the Gold Cost field means you can't afford the change."),
                new NuiSpacer { Height = 20f },
            }
        };
    }

    /// <summary>
    ///     Creates a section header label.
    /// </summary>
    private static NuiLabel CreateSectionHeader(string text)
    {
        return new NuiLabel(text)
        {
            Width = 580f,
            Height = 25f,
            ForegroundColor = new Color(205, 202, 71),
            HorizontalAlign = NuiHAlign.Left,
            VerticalAlign = NuiVAlign.Middle
        };
    }

    /// <summary>
    ///     Creates a sub-header label.
    /// </summary>
    private static NuiRow CreateSubHeader(string text)
    {
        return new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 20f },
                new NuiLabel (text)
                {
                    Width = 560f,
                    Height = 20f,
                    ForegroundColor = new Color(205, 202, 71),
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                }
            }
        };
    }

    /// <summary>
    ///     Creates a standard text label.
    /// </summary>
    private static NuiRow CreateText(string text)
    {
        return new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 40f },
                new NuiLabel (text)
                {
                    Width = 560f,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                }
            }
        };
    }
}
