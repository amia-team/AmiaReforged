using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesView : IScryView
{
    private const string RemovePropertyConst = "remove_property";

    public string RemoveProperty => RemovePropertyConst;

    public NuiBind<string> PropertyNames { get; } = new(key: "ip_names");
    public NuiBind<string> PropertyPowerCosts { get; } = new(key: "ip_power_costs");
    public NuiBind<bool> Removable { get; } = new(key: "ip_remove");
    public NuiBind<int> PropertyCount { get; } = new(key: "ip_count");

    public NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(PropertyNames)),
            new(new NuiGroup
            {
                Element = new NuiLabel(PropertyPowerCosts)
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

            new(new NuiButtonImage(resRef: "ir_cntrspell")
            {
                Id = RemovePropertyConst,
                Enabled = Removable
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
                new NuiList(cells, PropertyCount)
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