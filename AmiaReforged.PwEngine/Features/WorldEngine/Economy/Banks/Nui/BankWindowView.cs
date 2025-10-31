using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Front end for player bank interactions.
/// </summary>
public class BankWindowView : ScryView<BankWindowPresenter>
{
    public BankWindowView(NwPlayer player)
    {
        Presenter = new BankWindowPresenter(this, player);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override BankWindowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        throw new NotImplementedException();
    }
}


public class BankWindowPresenter(BankWindowView view, NwPlayer player) : ScryPresenter<BankWindowView>
{
    public override BankWindowView View { get; } = view;

    public override NuiWindowToken Token()
    {
        throw new NotImplementedException();
    }

    public override void InitBefore()
    {
        throw new NotImplementedException();
    }

    public override void Create()
    {
        throw new NotImplementedException();
    }

    public override void Close()
    {
        throw new NotImplementedException();
    }
}
