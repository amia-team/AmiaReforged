using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

/// <summary>
///     Represents an abstract base class for views in the Scry system.
/// </summary>
/// <typeparam name="TPresenter">The type of the presenter associated with the view.</typeparam>
[CreatedAtRuntime]
public abstract class ScryView<TPresenter> : IScryView where TPresenter : IScryPresenter
{
    /// <summary>
    ///     Gets or sets the presenter associated with the view.
    /// </summary>
    public abstract TPresenter Presenter { get; protected set; }

    /// <summary>
    ///     Gets the layout structure for the view.
    /// </summary>
    /// <returns>A NuiLayout object representing the layout of the view.</returns>
    public abstract NuiLayout RootLayout();
}
