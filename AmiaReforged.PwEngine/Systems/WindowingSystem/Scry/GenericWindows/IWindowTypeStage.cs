namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for specifying the type of window to build.
/// </summary>
public interface IWindowTypeStage
{
    /// <summary>
    ///     Creates a new instance of <see cref="SimplePopupBuilder" />.
    /// </summary>
    /// <returns>A new <see cref="SimplePopupBuilder" /> instance.</returns>
    ISimplePopupBuilder SimplePopup();
}