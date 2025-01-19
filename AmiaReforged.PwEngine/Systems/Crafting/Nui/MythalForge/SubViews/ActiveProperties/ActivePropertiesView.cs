using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesView : IScryView
{
    public IScryPresenter Presenter { get; }
    
    public NuiBind<string> PropertyNames { get; } = new("property_names");
    public NuiBind<string> PropertyPowerCosts { get; } = new("property_descriptions");
    public NuiBind<bool> Removable { get; } = new("property_enabled");
    public NuiBind<int> PropertyCount { get; } = new("property_count");

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
            }),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = "remove_property",
                Enabled = Removable
            })
            
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