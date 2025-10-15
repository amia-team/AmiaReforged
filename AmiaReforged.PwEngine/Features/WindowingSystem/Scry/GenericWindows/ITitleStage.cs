namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for setting the title of the popup window.
/// </summary>
public interface ITitleStage
{
    /// <summary>
    ///     Sets the message of the popup window.
    /// </summary>
    /// <param name="message">A message that will render in a NuiText field.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    IOpenStage WithMessage(string message);
}
