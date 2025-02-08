using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

/// <summary>
/// Represents an abstract base class for presenters in the Scry system.
/// </summary>
/// <typeparam name="TView">The type of the view associated with the presenter.</typeparam>
public abstract class ScryPresenter<TView> : IScryPresenter where TView : IScryView
{
    /// <summary>
    /// Gets the NuiWindowToken associated with the presenter.
    /// </summary>
    /// <returns>A NuiWindowToken object.</returns>
    public abstract NuiWindowToken Token(); 
    
    /// <summary>
    /// Gets the view associated with the presenter.
    /// </summary>
    public abstract TView View { get; }

    /// <summary>
    /// Initializes the presenter, setting up any necessary state or resources.
    /// </summary>
    public abstract void InitBefore();

    /// <summary>
    /// Handles input events from the module, which are passed to the presenter by <see cref="WindowDirector"/>.
    /// </summary>
    /// <param name="obj">The event object containing details about the input event.</param>
    public virtual void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Updates the view with any changes that need to be reflected in the user interface.
    /// </summary>
    public virtual void UpdateView()
    {
        // Default implementation does nothing.
    }

    /// <summary>
    /// Creates the view or any associated resources.
    /// </summary>
    public abstract void Create();

    /// <summary>
    /// Closes the presenter, cleaning up any resources or state.
    /// </summary>
    public abstract void Close();
}