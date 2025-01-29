using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;

/// <summary>
/// This is the view responsible for the changelist panel of the Mythal Forge. See <see cref="MythalForgeView"/>.
/// </summary>
public class ChangelistView : IScryView
{
    public NuiBind<string> PropertyLabel { get; } = new("change_label");
    public NuiBind<string> CostString { get; } = new("cost_string");
    public NuiBind<Color> Colors { get; } = new("changelist_colors");
    public const string RemoveFromChangeList = "remove_from_changelist";
    public string RemoveId => RemoveFromChangeList;
    public NuiBind<int> ChangeCount { get; } = new("change_count");
    public IScryPresenter Presenter { get; }

    public ChangelistView(IScryPresenter presenter)
    {
        Presenter = presenter;
    }

    /// <summary>
    /// Only concerned with building a NuiGroup for the changelist panel.
    /// </summary>
    /// <returns>A nui element intended only for use as an element of a larger view.</returns>
    public NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells = new()
        {
            new NuiListTemplateCell(new NuiLabel(PropertyLabel)
            {
                ForegroundColor = Colors
            }),
            new NuiListTemplateCell(new NuiGroup
            {
                Element = new NuiLabel(CostString)
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = RemoveFromChangeList,
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
                new NuiList(cells, ChangeCount)
            },
            Width = 400f,
            Height = 400f
        };
    }
}