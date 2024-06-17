namespace AmiaReforged.Settlements.Services.Economy.Initialization;

public interface IResourceInitializer
{
    /// <summary>
    /// If awaited, call NwTask.SwitchToMainThread() after calling this method to switch back to the game's main thread.
    /// </summary>
    /// <returns></returns>
    Task Initialize();
}