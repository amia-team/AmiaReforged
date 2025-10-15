using Anvil;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Builder class for creating generic windows.
/// </summary>
public class GenericWindowBuilder : IWindowBuilder, IWindowTypeStage
{
    /// <summary>
    ///     Returns the current instance for further configuration.
    /// </summary>
    /// <returns>The current <see cref="IWindowTypeStage" /> instance.</returns>
    public IWindowTypeStage For() => this;

    /// <summary>
    ///     Creates a new instance of <see cref="SimplePopupBuilder" /> and injects dependencies.
    /// </summary>
    /// <returns>A new <see cref="SimplePopupBuilder" /> instance.</returns>
    public ISimplePopupBuilder SimplePopup()
    {
        InjectionService? service = AnvilCore.GetService<InjectionService>();
        if (service == null)
        {
            LogManager.GetCurrentClassLogger().Error(message: "InjectionService is not available");
            return new SimplePopupBuilder();
        }

        SimplePopupBuilder popupBuilder = service.Inject(new SimplePopupBuilder());
        return popupBuilder;
    }
}
