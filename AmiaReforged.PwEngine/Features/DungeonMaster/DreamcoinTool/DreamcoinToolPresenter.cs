using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinTool;

public sealed class DreamcoinToolPresenter : ScryPresenter<DreamcoinToolView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override DreamcoinToolView View { get; }

    private readonly NwPlayer _dmPlayer;
    private readonly NwPlayer _targetPlayer;
    private readonly DreamcoinService _dreamcoinService;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public DreamcoinToolPresenter(DreamcoinToolView view, NwPlayer dmPlayer, NwPlayer targetPlayer, DreamcoinService dreamcoinService)
    {
        View = view;
        _dmPlayer = dmPlayer;
        _targetPlayer = targetPlayer;
        _dreamcoinService = dreamcoinService;
    }

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), "Dreamcoin Tool")
        {
            Geometry = new NuiRect(400f, 300f, View.GetWindowWidth(), View.GetWindowHeight()),
            Resizable = false,
        };

        if (!_dmPlayer.TryCreateNuiWindow(_window, out _token))
            return;

        // Set initial values
        RefreshBalanceDisplay();
        Token().SetBindValue(View.AddAmount, "0");
        Token().SetBindValue(View.TakeAmount, "0");
    }

    private async void RefreshBalanceDisplay()
    {
        string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
        int balance = await _dreamcoinService.GetDreamcoins(_targetPlayer.CDKey);

        await NwTask.SwitchToMainThread();

        Token().SetBindValue(View.TargetName, $"Target: {targetName}");
        Token().SetBindValue(View.CurrentBalance, $"Balance: {balance} DCs");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == DreamcoinToolView.AddButtonId)
        {
            HandleAddDc();
        }
        else if (ev.ElementId == DreamcoinToolView.TakeButtonId)
        {
            HandleTakeDc();
        }
    }

    private async void HandleAddDc()
    {
        string amountStr = Token().GetBindValue(View.AddAmount) ?? "0";
        if (!int.TryParse(amountStr, out int amount) || amount <= 0)
        {
            _dmPlayer.SendServerMessage("Please enter a valid positive number.");
            return;
        }

        int newBalance = await _dreamcoinService.AddDreamcoins(_targetPlayer.CDKey, amount);
        await NwTask.SwitchToMainThread();

        if (newBalance >= 0)
        {
            string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
            _dmPlayer.SendServerMessage($"Added {amount} DCs to {targetName}. New balance: {newBalance}");
            Log.Info($"DM {_dmPlayer.PlayerName} added {amount} DCs to {_targetPlayer.PlayerName}");
            RefreshBalanceDisplay();
            Token().SetBindValue(View.AddAmount, "0");
        }
        else
        {
            _dmPlayer.SendServerMessage("Failed to add DCs. Check logs for details.");
        }
    }

    private async void HandleTakeDc()
    {
        string amountStr = Token().GetBindValue(View.TakeAmount) ?? "0";
        if (!int.TryParse(amountStr, out int amount) || amount <= 0)
        {
            _dmPlayer.SendServerMessage("Please enter a valid positive number.");
            return;
        }

        int newBalance = await _dreamcoinService.RemoveDreamcoins(_targetPlayer.CDKey, amount);
        await NwTask.SwitchToMainThread();

        if (newBalance >= 0)
        {
            string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
            _dmPlayer.SendServerMessage($"Removed {amount} DCs from {targetName}. New balance: {newBalance}");
            Log.Info($"DM {_dmPlayer.PlayerName} removed {amount} DCs from {_targetPlayer.PlayerName}");
            RefreshBalanceDisplay();
            Token().SetBindValue(View.TakeAmount, "0");
        }
        else
        {
            _dmPlayer.SendServerMessage("Failed to remove DCs. Player may not have enough.");
        }
    }

    public override void Close()
    {
        _token.Close();
    }
}
