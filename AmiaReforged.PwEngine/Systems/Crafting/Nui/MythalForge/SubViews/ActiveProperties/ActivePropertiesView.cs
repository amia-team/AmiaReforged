using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesView : IScryView
{
    private const string RemovePropertyConst = "remove_property";
    
    public string RemoveProperty => RemovePropertyConst;
    public IScryPresenter Presenter { get; }
    
    public NuiBind<string> PropertyNames { get; } = new("ip_names");
    public NuiBind<string> PropertyPowerCosts { get; } = new("ip_power_costs");
    public NuiBind<bool> Removable { get; } = new("ip_remove");
    public NuiBind<int> PropertyCount { get; } = new("ip_count");
    

    public ActivePropertiesView(IScryPresenter presenter)
    {
        Presenter = presenter;
    }

    public NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells = new()
        {
            new NuiListTemplateCell(new NuiLabel(PropertyNames)),
            new NuiListTemplateCell(new NuiGroup
            {
                Element = new NuiLabel(PropertyPowerCosts)
            })
            {
                Width = 30f,
                VariableSize = false
            },
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = RemovePropertyConst,
                Enabled = Removable
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
                new NuiList(cells, PropertyCount)
            },
            Width = 400f,
            Height = 400f
        };
    }
}