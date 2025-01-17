using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ActiveProperties;

public class ActivePropertiesView : IScryView
{
    public IScryPresenter Presenter { get; }

    public ActivePropertiesView(IScryPresenter presenter)
    {
        Presenter = presenter;
    }

    public NuiLayout RootLayout()
    {
        throw new NotImplementedException();
    }
}