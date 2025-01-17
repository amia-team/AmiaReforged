using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;

public abstract class ScryPresenter<TView> : IScryPresenter where TView : IScryView
{
    public abstract NuiWindowToken Token(); 
    
    public abstract TView View { get; }

    public abstract void Initialize();

    public abstract void UpdateView();

    public abstract void Create();

    public abstract void Close();
}