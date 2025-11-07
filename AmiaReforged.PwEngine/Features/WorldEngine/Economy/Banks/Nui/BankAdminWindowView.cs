using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Bank admin window for account management - placeholder for future implementation.
/// </summary>
public sealed class BankAdminWindowView : ScryView<BankAdminWindowPresenter>
{
    public override BankAdminWindowPresenter Presenter { get; protected set; }

    public BankAdminWindowView(NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        Presenter = new BankAdminWindowPresenter(this, player, coinhouseTag, bankDisplayName);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiLabel("Admin Window")
                {
                    Height = 40f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel("Account management features coming soon.")
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer(),
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "admin_btn_close",
                            Width = 110f,
                            Height = 32f
                        },
                        new NuiSpacer()
                    ]
                }
            ]
        };
    }
}

public sealed class BankAdminWindowPresenter : ScryPresenter<BankAdminWindowView>
{
    private readonly NwPlayer _player;
    private readonly string _bankDisplayName;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public BankAdminWindowPresenter(BankAdminWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _bankDisplayName = bankDisplayName;
    }

    public override BankAdminWindowView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), $"{_bankDisplayName} - Administration")
        {
            Geometry = new NuiRect(180f, 180f, 400f, 300f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null) InitBefore();
        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }
}
