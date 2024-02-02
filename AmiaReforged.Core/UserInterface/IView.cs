using Anvil.API;

namespace AmiaReforged.Core.UserInterface;

public interface IView
{
    public string Id { get; }

    public string Title { get; }

    public IViewModel CreateDefaultViewModel(NwPlayer player);
}

public interface IViewModel
{
    public string State { get; set; }

    public void UpdateView();
}