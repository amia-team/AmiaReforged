using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

public interface IScryWindow
{
    public NuiWindowToken Token { get; }
    public IScryView View { get; }
    public void InitializeWindow();
    public void CloseWindow();
}