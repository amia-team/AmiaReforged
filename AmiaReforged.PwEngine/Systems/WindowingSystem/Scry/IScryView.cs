using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

/// <summary>
/// Defines the contract for view classes in the Scry system.
/// </summary>
public interface IScryView
{
    /// <summary>
    /// Gets the layout structure for the view.
    /// </summary>
    /// <returns>A NuiLayout object representing the layout of the view.</returns>
    public NuiLayout RootLayout();
}