using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem;

/// <summary>
/// Internal interface - implement <see cref="NuiView{TView}"/> instead.
/// </summary>
public interface INuiView
{
    public string Id { get; }
    public string Title { get; }
    public INuiController? CreateDefaultController(NwPlayer player);
}