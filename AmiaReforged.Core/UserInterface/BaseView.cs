using Anvil.API;

namespace AmiaReforged.Core.UserInterface;

public abstract class BaseView
{
    public BaseViewModel ViewModel { get; set; } = null!;
    
    public abstract NuiLayout GetViewLayout(); 

    public abstract void Update();
}