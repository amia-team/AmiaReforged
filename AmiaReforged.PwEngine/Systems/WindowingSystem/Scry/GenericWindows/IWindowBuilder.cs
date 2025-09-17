namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for building windows.
/// </summary>
public interface IWindowBuilder
{
    /// <summary>
    ///     Returns the current instance for further configuration.
    /// </summary>
    /// <returns>The current <see cref="IWindowTypeStage" /> instance.</returns>
    IWindowTypeStage For();
}