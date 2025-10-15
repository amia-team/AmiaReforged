using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Interface for building simple popup windows.
/// </summary>
public interface ISimplePopupBuilder
{
    /// <summary>
    ///     Sets the player to whom the popup window will be displayed.
    /// </summary>
    /// <param name="player">The player in question.</param>
    /// <returns>The current <see cref="IPlayerStage" /> instance.</returns>
    IPlayerStage WithPlayer(NwPlayer player);
}
