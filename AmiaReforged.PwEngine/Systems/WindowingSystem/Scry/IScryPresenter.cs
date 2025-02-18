using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

/// <summary>
/// Defines the contract for presenter classes in the Scry system.
/// </summary>
public interface IScryPresenter
{
    /// <summary>
    /// Gets the NuiWindowToken associated with the presenter.
    /// </summary>
    /// <returns>A NuiWindowToken object.</returns>
    public NuiWindowToken Token();

    /// <summary>
    /// Initializes the presenter, setting up any necessary state or resources.
    /// </summary>
    public void InitBefore();

    public void ProcessEvent(ModuleEvents.OnNuiEvent obj);
    
    /// <summary>
    /// Updates the view with any changes that need to be reflected in the user interface.
    /// </summary>
    public void UpdateView();

    /// <summary>
    /// Creates the view or any associated resources.
    /// </summary>
    public void Create();

    /// <summary>
    /// Closes the presenter, cleaning up any resources or state.
    /// </summary>
    public void Close();
}