using AmiaReforged.Core.Models;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.BanManager;

/// <summary>
/// Presenter for the Ban Manager DM tool window.
/// </summary>
public sealed class BanManagerPresenter : ScryPresenter<BanManagerView>
{
    public override BanManagerView View { get; }

    private readonly NwPlayer _dmPlayer;
    private readonly BanManagerModel _model;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public BanManagerPresenter(BanManagerView view, NwPlayer dmPlayer)
    {
        View = view;
        _dmPlayer = dmPlayer;
        _model = new BanManagerModel(dmPlayer);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(_model);

        _model.OnBansUpdated += OnBansUpdated;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(100f, 100f, View.GetWindowWidth(), View.GetWindowHeight()),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window is null) InitBefore();

        if (_window is null)
        {
            _dmPlayer.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.", ColorConstants.Orange);
            return;
        }

        if (!_dmPlayer.TryCreateNuiWindow(_window, out _token))
            return;

        // Initialize binds
        Token().SetBindValue(View.NewCdKey, "");
        Token().SetBindValue(View.SearchTerm, "");

        // Load initial data
        LoadBansAsync();
    }

    private async void LoadBansAsync()
    {
        await _model.LoadBansAsync();
        await NwTask.SwitchToMainThread();
        RefreshBanList();
    }

    private void OnBansUpdated(BanManagerModel sender, EventArgs e)
    {
        NwTask.Run(async () =>
        {
            await NwTask.SwitchToMainThread();
            RefreshBanList();
        });
    }

    private void RefreshBanList()
    {
        List<string> cdKeys = _model.VisibleBans.Select(b => b.CdKey).ToList();

        Token().SetBindValues(View.BanCdKeys, cdKeys);
        Token().SetBindValue(View.BanCount, _model.VisibleBans.Count);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        switch (ev.ElementId)
        {
            case BanManagerView.BanButtonId:
                HandleBan();
                break;

            case BanManagerView.TargetBanButtonId:
                HandleTargetBan();
                break;

            case BanManagerView.SearchButtonId:
                HandleSearch();
                break;

            case BanManagerView.UnbanButtonId:
                HandleUnban(ev.ArrayIndex);
                break;
        }
    }

    private async void HandleBan()
    {
        string cdKey = Token().GetBindValue(View.NewCdKey) ?? "";

        if (string.IsNullOrWhiteSpace(cdKey))
        {
            _dmPlayer.SendServerMessage("Please enter a CD Key to ban.", ColorConstants.Red);
            return;
        }

        bool success = await _model.BanCdKeyAsync(cdKey);
        await NwTask.SwitchToMainThread();

        if (success)
        {
            Token().SetBindValue(View.NewCdKey, "");
        }
    }

    private void HandleTargetBan()
    {
        _model.EnterTargetingMode(async cdKey =>
        {
            await _model.BanCdKeyAsync(cdKey);
        });
    }

    private void HandleSearch()
    {
        string searchTerm = Token().GetBindValue(View.SearchTerm) ?? "";
        _model.SetSearchTerm(searchTerm);
    }

    private async void HandleUnban(int arrayIndex)
    {
        if (arrayIndex < 0 || arrayIndex >= _model.VisibleBans.Count)
            return;

        Ban ban = _model.VisibleBans[arrayIndex];
        await _model.UnbanCdKeyAsync(ban.CdKey);
    }

    public override void Close()
    {
    }
}
