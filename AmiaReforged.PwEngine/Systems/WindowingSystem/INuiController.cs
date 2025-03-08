using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem;

/// <summary>
///     Internal interface - implement <see cref="NuiController{TView}" /> instead.
/// </summary>
public interface INuiController
{
    public NuiWindowToken Token { get; init; }

    public bool AutoClose { get; }

    public void Init();

    public void ProcessEvent(ModuleEvents.OnNuiEvent eventData);

    public void Close(bool destroyWindow = true);
}