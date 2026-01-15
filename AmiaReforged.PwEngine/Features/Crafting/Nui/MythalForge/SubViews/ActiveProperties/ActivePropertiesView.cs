using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesView : IScryView
{
    private const string RemovePropertyConst = "remove_property";

    public string RemoveProperty => RemovePropertyConst;

    public NuiBind<string> PropertyNames { get; } = new(key: "ip_names");
    public NuiBind<string> PropertyPowerCosts { get; } = new(key: "ip_power_costs");
    public NuiBind<Color> PropertyColors { get; } = new(key: "ip_colors");
    public NuiBind<bool> Removable { get; } = new(key: "ip_remove");
    public NuiBind<int> PropertyCount { get; } = new(key: "ip_count");

    public NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(PropertyNames)
            {
                Width = 400f,
                ForegroundColor = PropertyColors
            }),
            new(new NuiLabel(PropertyPowerCosts)
                {
                    Width = 15f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = PropertyColors
                }),

            new(new NuiButtonImage(resRef: "ir_cntrspell")
            {
                Id = RemovePropertyConst,
                Enabled = Removable
            })
            {
                Width = 25f,
                VariableSize = false
            }
        ];

        return new NuiColumn
        {
            Children =
            {
                new NuiList(cells, PropertyCount)
                {
                    RowHeight = 25f
                }
            },
            Width = 360f,
            Height = 250f
        };
    }
}
