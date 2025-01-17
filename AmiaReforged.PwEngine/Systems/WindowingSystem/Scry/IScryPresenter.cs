using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

public interface IScryPresenter
{
    public NuiWindowToken Token();
    public void Initialize();
    public void UpdateView();

    public void Create();
    public void Close();
}