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
            new NuiListTemplateCell(new NuiLabel(PropertyNames)
            {
                Tooltip = PropertyNames,
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Left,
                ForegroundColor = PropertyColors
            }) { Width = 270f },
            new NuiListTemplateCell(new NuiLabel(PropertyPowerCosts)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Center,
                ForegroundColor = PropertyColors
            }) { Width = 25f },
            new NuiListTemplateCell(new NuiButtonImage(resRef: "ui_btn_forgerem")
            {
                Id = RemovePropertyConst,
                Tooltip = "Remove this property.",
                Width = 25f,
                Height = 25f,
                Enabled = Removable
            }) { Width = 25f },
            new NuiListTemplateCell(new NuiSpacer
            {
                Width = 35f,
            }) { Width = 40f }
        ];

        return new NuiColumn
        {
            Children =
            {
                new NuiList(cells, PropertyCount)
                {
                    RowHeight = 25f,
                    Width = 360f,
                    Height = 250f
                }
            }
        };
    }
}
