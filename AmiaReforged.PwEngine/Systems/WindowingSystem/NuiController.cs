using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem;

public abstract class NuiController<TView> : INuiController where TView : NuiView<TView>, new()
{
    private const string UseWindowPermissionKeyFormat = "toolbox.window.use.{0}.{1}";


    /// <summary>
    ///     The associated view for this window controller.
    /// </summary>
    public TView View { protected get; init; }

    /// <summary>
    ///     The associated <see cref="NuiWindowToken" /> for this window controller.
    /// </summary>
    public NuiWindowToken Token { get; init; }

    /// <summary>
    ///     Gets or sets if the window should be automatically closed if the player moves.
    /// </summary>
    public virtual bool AutoClose { get; set; } = false;

    public abstract void Init();

    public abstract void ProcessEvent(ModuleEvents.OnNuiEvent eventData);

    public void Close(bool destroyWindow = true)
    {
        OnClose();

        if (destroyWindow) Token.Dispose();
    }

    protected abstract void OnClose();

    protected void ApplyPermissionBindings(params NuiBind<bool>[] binds)
    {
        //Unused for now.
    }
}