using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

public sealed class PlayerDcSelfPresenter : ScryPresenter<PlayerDcSelfView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override PlayerDcSelfView View { get; }

    private readonly NwPlayer _player;
    private readonly DreamcoinService _dreamcoinService;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public PlayerDcSelfPresenter(PlayerDcSelfView view, NwPlayer player, DreamcoinService dreamcoinService)
    {
        View = view;
        _player = player;
        _dreamcoinService = dreamcoinService;
    }

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), "Dreamcoins")
        {
            Geometry = new NuiRect(400f, 300f, View.GetWindowWidth(), View.GetWindowHeight()),
            Resizable = false,
        };

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        RefreshDisplay();
    }

    private async void RefreshDisplay()
    {
        int balance = await _dreamcoinService.GetDreamcoins(_player.CDKey);
        await NwTask.SwitchToMainThread();

        NwCreature? creature = _player.LoginCreature;
        int level = creature?.Level ?? 1;
        int gold = 1500 * level;
        int xp = level <= 20 ? level * 1000 : level * 500;
        int goldOnly = 2500 * level;

        bool hasPcKey = creature?.Inventory.Items.Any(i => i.ResRef == "ds_pckey") ?? false;
        bool isMaxLevel = level >= 30;
        bool canBurnForXp = hasPcKey && !isMaxLevel;

        string burnRewardText = isMaxLevel ? "Already at max level" :
            (hasPcKey ? $"Burn reward: {gold} gold, {xp} XP" : "Requires ds_pckey to burn for XP");

        Token().SetBindValue(View.CurrentBalance, $"You have {balance} Dreamcoins");
        Token().SetBindValue(View.BurnRewardInfo, burnRewardText);
        Token().SetBindValue(View.BurnGoldOnlyInfo, $"Gold only reward: {goldOnly} gold");
        Token().SetBindValue(View.CanBurnForXp, canBurnForXp);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == PlayerDcSelfView.BurnButtonId)
        {
            HandleBurnDc();
        }
        else if (ev.ElementId == PlayerDcSelfView.BurnGoldOnlyButtonId)
        {
            HandleBurnDcGoldOnly();
        }
    }

    private async void HandleBurnDc()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.");
            return;
        }

        bool hasPcKey = creature.Inventory.Items.Any(i => i.ResRef == "ds_pckey");
        if (!hasPcKey)
        {
            _player.SendServerMessage("You need a ds_pckey item to burn Dreamcoins for XP. Use 'Burn for Gold Only' instead.");
            return;
        }

        int balance = await _dreamcoinService.GetDreamcoins(_player.CDKey);
        await NwTask.SwitchToMainThread();

        if (balance < 1)
        {
            _player.SendServerMessage("You don't have any Dreamcoins to burn.");
            return;
        }

        int level = creature.Level;
        int currentXp = creature.Xp;
        int currentLevelXp = (level * (level - 1) / 2) * 1000;
        int nextLevelXp = level * 1000;
        int xpForNextLevel = currentLevelXp + nextLevelXp;

        if (level < 30 && currentXp >= xpForNextLevel)
        {
            _player.SendServerMessage("Take your current level before burning another Dreamcoin.");
            return;
        }

        uint gold = (uint)(1500 * level);
        int xp = level <= 20 ? level * 1000 : level * 500;

        int newBalance = await _dreamcoinService.RemoveDreamcoins(_player.CDKey, 1);
        await NwTask.SwitchToMainThread();

        if (newBalance >= 0)
        {
            creature.Gold += gold;
            creature.Xp += xp;

            _player.SendServerMessage($"Burned 1 DC! Received {gold} gold and {xp} XP. Balance: {newBalance} DCs");
            Log.Info($"Player {_player.PlayerName} burned 1 DC for {gold} gold and {xp} XP");
            RefreshDisplay();
        }
        else
        {
            _player.SendServerMessage("Failed to burn DC. Please try again.");
        }
    }

    private async void HandleBurnDcGoldOnly()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.");
            return;
        }

        int balance = await _dreamcoinService.GetDreamcoins(_player.CDKey);
        await NwTask.SwitchToMainThread();

        if (balance < 1)
        {
            _player.SendServerMessage("You don't have any Dreamcoins to burn.");
            return;
        }

        int level = creature.Level;
        uint gold = (uint)(2500 * level);

        int newBalance = await _dreamcoinService.RemoveDreamcoins(_player.CDKey, 1);
        await NwTask.SwitchToMainThread();

        if (newBalance >= 0)
        {
            creature.Gold += gold;

            _player.SendServerMessage($"Burned 1 DC! Received {gold} gold. Balance: {newBalance} DCs");
            Log.Info($"Player {_player.PlayerName} burned 1 DC for {gold} gold (gold only)");
            RefreshDisplay();
        }
        else
        {
            _player.SendServerMessage("Failed to burn DC. Please try again.");
        }
    }

    public override void Close()
    {
        _token.Close();
    }
}
