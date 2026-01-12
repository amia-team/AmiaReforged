namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

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

    /// <summary>
    ///     Creates a new instance of <see cref="ConfirmationPopupBuilder" />.
    /// </summary>
    /// <returns>A new <see cref="ConfirmationPopupBuilder" /> instance.</returns>
    IConfirmationPopupBuilder ConfirmationPopup();
}
