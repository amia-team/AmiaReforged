using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Bank storage window for personal item storage - placeholder for future implementation.
/// </summary>
public sealed class BankStorageWindowView : ScryView<BankStorageWindowPresenter>
{
    public override BankStorageWindowPresenter Presenter { get; protected set; }

    public BankStorageWindowView(NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        Presenter = new BankStorageWindowPresenter(this, player, coinhouseTag, bankDisplayName);
    }

    public override NuiLayout RootLayout()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiLabel("Storage Window")
                {
                    Height = 40f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel("Item storage features coming soon.")
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
                            Id = "storage_btn_close",
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

public sealed class BankStorageWindowPresenter : ScryPresenter<BankStorageWindowView>
{
    private readonly NwPlayer _player;
    private readonly string _bankDisplayName;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public BankStorageWindowPresenter(BankStorageWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _bankDisplayName = bankDisplayName;
    }

    public override BankStorageWindowView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), $"{_bankDisplayName} - Storage")
        {
            Geometry = new NuiRect(160f, 160f, 400f, 300f),
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
