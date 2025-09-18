namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for setting the player of the popup window.
/// </summary>
public interface IPlayerStage
{
    /// <summary>
    ///     Sets the title of the popup window.
    /// </summary>
    /// <param name="title">The title of the popup window.</param>
    /// <returns>The current <see cref="ITitleStage" /> instance.</returns>
    ITitleStage WithTitle(string title);
}