using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DomainChanger;

[ServiceBinding(typeof(DomainChangerDirector))]
public sealed class DomainChangerDirector
{
    private readonly WindowDirector _windowDirector;

    public DomainChangerDirector(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    public void OpenDomainChanger(NwPlayer player)
    {
        DomainChangerView view = new();
        DomainChangerPresenter presenter = new(view, player);
        _windowDirector.OpenWindow(presenter);
    }
}
