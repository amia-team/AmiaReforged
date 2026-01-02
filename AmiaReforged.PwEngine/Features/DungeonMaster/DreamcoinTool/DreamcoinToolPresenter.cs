using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinTool;

public sealed class DreamcoinToolPresenter : ScryPresenter<DreamcoinToolView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int VfxFreedomOfMovement = 3;

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
        else if (ev.ElementId == DreamcoinToolView.AddPartyButtonId)
        {
            HandleAddDcParty();
        }
        else if (ev.ElementId == DreamcoinToolView.AddNearbyButtonId)
        {
            HandleAddDcNearby();
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
            ApplyDcReceivedEffect(_targetPlayer, amount);
            RefreshBalanceDisplay();
            Token().SetBindValue(View.AddAmount, "0");
        }
        else
        {
            _dmPlayer.SendServerMessage("Failed to add DCs. Check logs for details.");
        }
    }

    private async void HandleAddDcParty()
    {
        string amountStr = Token().GetBindValue(View.AddAmount) ?? "0";
        if (!int.TryParse(amountStr, out int amount) || amount <= 0)
        {
            _dmPlayer.SendServerMessage("Please enter a valid positive number.");
            return;
        }

        NwCreature? targetCreature = _targetPlayer.LoginCreature;
        if (targetCreature == null)
        {
            _dmPlayer.SendServerMessage("Target player not found.");
            return;
        }

        NwArea? targetArea = targetCreature.Area;
        if (targetArea == null)
        {
            _dmPlayer.SendServerMessage("Target is not in a valid area.");
            return;
        }

        // Get party members in the same area
        List<NwPlayer> partyMembers = new();
        foreach (NwCreature member in targetCreature.Faction.GetMembers())
        {
            if (member.IsPlayerControlled(out NwPlayer? player) && member.Area == targetArea)
            {
                partyMembers.Add(player);
            }
        }

        int successCount = 0;
        List<NwPlayer> successfulPlayers = new();
        foreach (NwPlayer player in partyMembers)
        {
            int result = await _dreamcoinService.AddDreamcoins(player.CDKey, amount);
            if (result >= 0)
            {
                successCount++;
                successfulPlayers.Add(player);
            }
        }

        await NwTask.SwitchToMainThread();

        foreach (NwPlayer player in successfulPlayers)
        {
            ApplyDcReceivedEffect(player, amount);
        }

        _dmPlayer.SendServerMessage($"Added {amount} DCs to {successCount} party members in the area.");
        Log.Info($"DM {_dmPlayer.PlayerName} added {amount} DCs to {successCount} party members");
        RefreshBalanceDisplay();
        Token().SetBindValue(View.AddAmount, "0");
    }

    private async void HandleAddDcNearby()
    {
        string amountStr = Token().GetBindValue(View.AddAmount) ?? "0";
        if (!int.TryParse(amountStr, out int amount) || amount <= 0)
        {
            _dmPlayer.SendServerMessage("Please enter a valid positive number.");
            return;
        }

        NwCreature? targetCreature = _targetPlayer.LoginCreature;
        if (targetCreature == null)
        {
            _dmPlayer.SendServerMessage("Target player not found.");
            return;
        }

        // Get players within 5 meters
        List<NwPlayer> nearbyPlayers = new();
        foreach (NwCreature creature in targetCreature.Location!.GetNearestCreatures()
            .Where(c => c.IsPlayerControlled(out _) && c.Distance(targetCreature) <= 5.0f))
        {
            if (creature.IsPlayerControlled(out NwPlayer? player))
            {
                nearbyPlayers.Add(player);
            }
        }

        int successCount = 0;
        List<NwPlayer> successfulPlayers = new();
        foreach (NwPlayer player in nearbyPlayers)
        {
            int result = await _dreamcoinService.AddDreamcoins(player.CDKey, amount);
            if (result >= 0)
            {
                successCount++;
                successfulPlayers.Add(player);
            }
        }

        await NwTask.SwitchToMainThread();

        foreach (NwPlayer player in successfulPlayers)
        {
            ApplyDcReceivedEffect(player, amount);
        }

        _dmPlayer.SendServerMessage($"Added {amount} DCs to {successCount} nearby players.");
        Log.Info($"DM {_dmPlayer.PlayerName} added {amount} DCs to {successCount} nearby players");
        RefreshBalanceDisplay();
        Token().SetBindValue(View.AddAmount, "0");
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

    private void ApplyDcReceivedEffect(NwPlayer player, int amount)
    {
        NwCreature? creature = player.LoginCreature;
        if (creature == null) return;

        // Apply visual effect
        Effect vfx = Effect.VisualEffect((VfxType)VfxFreedomOfMovement, false, 1.0f);
        creature.ApplyEffect(EffectDuration.Temporary, vfx, TimeSpan.FromSeconds(3));

        // Send combat log message
        string dcWord = amount == 1 ? "Dreamcoin" : "Dreamcoins";
        player.SendServerMessage($"You received {amount} {dcWord}!", ColorConstants.Yellow);
    }

    public override void Close()
    {
        _token.Close();
    }
}
