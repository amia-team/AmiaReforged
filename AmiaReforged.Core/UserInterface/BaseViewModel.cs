namespace AmiaReforged.Core.UserInterface;

public abstract class BaseViewModel
{
    public abstract string State { get; set; }

    public abstract void UpdateView();
}