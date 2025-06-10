using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;

/// <summary>
///     This is the view responsible for the changelist panel of the Mythal Forge. See <see cref="MythalForgeView" />.
/// </summary>
public class ChangelistView : IScryView
{
    public const string RemoveFromChangeList = "remove_from_changelist";

    public ChangelistView(IScryPresenter presenter)
    {
    }

    public NuiBind<string> PropertyLabel { get; } = new(key: "change_label");
    public NuiBind<string> CostString { get; } = new(key: "cost_string");
    public NuiBind<Color> Colors { get; } = new(key: "changelist_colors");
    public string RemoveId => RemoveFromChangeList;
    public NuiBind<int> ChangeCount { get; } = new(key: "change_count");

    /// <summary>
    ///     Only concerned with building a NuiGroup for the changelist panel.
    /// </summary>
    /// <returns>A nui element intended only for use as an element of a larger view.</returns>
    public NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(PropertyLabel)
            {
                ForegroundColor = Colors
            }),

            new(new NuiGroup
            {
                Element = new NuiLabel(CostString)
                {
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                Aspect = 1f
            })
            {
                Width = 30f,
                VariableSize = false
            },

            new(new NuiButtonImage(resRef: "ir_abort")
            {
                Id = RemoveFromChangeList,
                Aspect = 1f
            })
            {
                Width = 30f,
                VariableSize = false
            }
        ];
        return new NuiColumn
        {
            Children =
            {
                new NuiList(cells, ChangeCount)
                {
                    RowHeight = 30f,
                    Scrollbars = NuiScrollbars.None
                }
            },
            Width = 400f,
            Height = 400f
        };
    }
}