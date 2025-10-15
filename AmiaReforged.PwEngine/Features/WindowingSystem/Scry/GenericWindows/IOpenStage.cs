using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for opening the popup window and setting additional options.
/// </summary>
public interface IOpenStage
{
    /// <summary>
    ///     Opens the popup window.
    /// </summary>
    void Open();

    /// <summary>
    ///     Sets the token for the popup window.
    /// </summary>
    /// <param name="token">The token of the parent window.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    void OpenWithParent(NuiWindowToken token);

    /// <summary>
    ///     Enables the ignore button for the popup window.
    /// </summary>
    /// <param name="ignoreTag">The tag for the local int that will be stored to the character's PC Key.</param>
    /// <returns>The current <see cref="IOpenStage" /> instance.</returns>
    IOpenStage EnableIgnoreButton(string ignoreTag);
}
