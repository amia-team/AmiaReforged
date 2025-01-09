using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

public interface IWindow
{
    public string Title { get; }

    /// <summary>
    /// Returns the window's token.
    /// </summary>
    public NuiWindowToken GetToken();

    /// <summary>
    /// Run before the NuiWindow is ever created.
    /// </summary>
    public void Init();
    
    /// <summary>
    /// Runs when the window is closed.
    /// </summary>
    public void Close();
    
    public void Create();
}