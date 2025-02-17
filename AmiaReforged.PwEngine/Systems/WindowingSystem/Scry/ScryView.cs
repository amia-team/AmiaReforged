using Anvil.API;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

/// <summary>
/// Represents an abstract base class for views in the Scry system.
/// </summary>
/// <typeparam name="TPresenter">The type of the presenter associated with the view.</typeparam>
public abstract class ScryView<TPresenter> : IScryView where TPresenter : IScryPresenter
{
    /// <summary>
    /// Gets or sets the presenter associated with the view.
    /// </summary>
    public abstract TPresenter Presenter { get; protected set; }

    /// <summary>
    /// Gets the layout structure for the view.
    /// </summary>
    /// <returns>A NuiLayout object representing the layout of the view.</returns>
    public abstract NuiLayout RootLayout();
}