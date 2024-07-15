using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem;

[ServiceBinding(typeof(INuiView))]
public abstract class NuiView<TView> : INuiView
    where TView : NuiView<TView>, new()
{
    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract NuiWindow? WindowTemplate { get; }
    public abstract INuiController? CreateDefaultController(NwPlayer player);

    protected T? CreateController<T>(NwPlayer player) where T : NuiController<TView>, new()
    {
        if (player.TryCreateNuiWindow(WindowTemplate, out NuiWindowToken token))
        {
            return new T
            {
                View = (TView)this,
                Token = token,
            };
        }

        return null;
    }
}