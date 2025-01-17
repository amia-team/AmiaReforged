using Anvil.API;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

public abstract class ScryView<TPresenter> : IScryView where TPresenter : IScryPresenter
{
    public abstract TPresenter Presenter { get; protected set; }
    public abstract NuiLayout RootLayout();
}