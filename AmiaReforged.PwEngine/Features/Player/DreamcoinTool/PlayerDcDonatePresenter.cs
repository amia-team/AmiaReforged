using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.DreamcoinTool;

public sealed class PlayerDcDonatePresenter : ScryPresenter<PlayerDcDonateView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public override PlayerDcDonateView View { get; }

    private readonly NwPlayer _player;
    private readonly NwPlayer _targetPlayer;
    private readonly DreamcoinService _dreamcoinService;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override NuiWindowToken Token() => _token;

    public PlayerDcDonatePresenter(PlayerDcDonateView view, NwPlayer player, NwPlayer targetPlayer, DreamcoinService dreamcoinService)
    {
        View = view;
        _player = player;
        _targetPlayer = targetPlayer;
        _dreamcoinService = dreamcoinService;
    }

    public override void InitBefore()
    {
    }

    public override void Create()
    {
        _window = new NuiWindow(View.RootLayout(), "Player Interaction")
        {
            Geometry = new NuiRect(400f, 300f, View.GetWindowWidth(), View.GetWindowHeight()),
            Resizable = true,
        };

        if (!_player.TryCreateNuiWindow(_window, out _token))
            return;

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
        Token().SetBindValue(View.TargetName, $"Target: {targetName}");

        // Check cooldown
        TimeSpan? remaining = GetDonateCooldownRemaining();
        if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
        {
            int minutes = (int)remaining.Value.TotalMinutes;
            Token().SetBindValue(View.DonateStatus, $"Donate available in {minutes} min");
        }
        else
        {
            Token().SetBindValue(View.DonateStatus, "You can donate 1 DC");
        }
    }

    private string GetCooldownVariableName()
    {
        return $"{_targetPlayer.CDKey}_lastdonation";
    }

    private TimeSpan? GetDonateCooldownRemaining()
    {
        NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
        if (pcKey == null) return null;

        string variableName = GetCooldownVariableName();
        string? timestampStr = pcKey.GetObjectVariable<LocalVariableString>(variableName).Value;
        if (string.IsNullOrEmpty(timestampStr)) return null;

        if (!DateTime.TryParse(timestampStr, out DateTime lastDonate)) return null;

        TimeSpan elapsed = DateTime.UtcNow - lastDonate;
        TimeSpan cooldown = TimeSpan.FromHours(1);

        if (elapsed >= cooldown) return null;

        return cooldown - elapsed;
    }

    private void SetDonateCooldown()
    {
        NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
        if (pcKey == null) return;

        string variableName = GetCooldownVariableName();
        pcKey.GetObjectVariable<LocalVariableString>(variableName).Value = DateTime.UtcNow.ToString("O");
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click)
            return;

        if (ev.ElementId == PlayerDcDonateView.DonateButtonId)
        {
            HandleDonate();
        }
        else if (ev.ElementId == PlayerDcDonateView.RecommendButtonId)
        {
            HandleRecommend();
        }
    }

    private async void HandleDonate()
    {
        // Check cooldown
        TimeSpan? remaining = GetDonateCooldownRemaining();
        if (remaining.HasValue && remaining.Value.TotalSeconds > 0)
        {
            int minutes = (int)remaining.Value.TotalMinutes;
            _player.SendServerMessage($"You must wait {minutes} more minutes before donating again.");
            return;
        }

        // Check balance
        int balance = await _dreamcoinService.GetDreamcoins(_player.CDKey);
        await NwTask.SwitchToMainThread();

        if (balance < 1)
        {
            _player.SendServerMessage("You don't have any Dreamcoins to donate.");
            return;
        }

        // Remove from donor, add to recipient
        int newDonorBalance = await _dreamcoinService.RemoveDreamcoins(_player.CDKey, 1);
        await NwTask.SwitchToMainThread();

        if (newDonorBalance < 0)
        {
            _player.SendServerMessage("Failed to donate DC. Please try again.");
            return;
        }

        int newRecipientBalance = await _dreamcoinService.AddDreamcoins(_targetPlayer.CDKey, 1);
        await NwTask.SwitchToMainThread();

        if (newRecipientBalance < 0)
        {
            // Refund the donor
            await _dreamcoinService.AddDreamcoins(_player.CDKey, 1);
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage("Failed to donate DC to recipient. Your DC has been refunded.");
            return;
        }

        SetDonateCooldown();

        string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
        string donorName = _player.LoginCreature?.Name ?? "Unknown";

        _player.SendServerMessage($"You donated 1 DC to {targetName}. Your balance: {newDonorBalance}");
        _targetPlayer.SendServerMessage($"{donorName} donated 1 DC to you!");

        Log.Info($"Player {_player.PlayerName} donated 1 DC to {_targetPlayer.PlayerName}");

        RefreshDisplay();
    }

    private void HandleRecommend()
    {
        string targetName = _targetPlayer.LoginCreature?.Name ?? "Unknown";
        string recommenderName = _player.LoginCreature?.Name ?? "Unknown";

        foreach (NwPlayer dm in NwModule.Instance.Players.Where(p => p.IsDM))
        {
            dm.SendServerMessage($"{recommenderName} has recommended {targetName} for good roleplay!", ColorConstants.Cyan);
        }

        _player.FloatingTextString($"You have recommended {targetName} for good roleplay.", false);
        Log.Info($"Player {_player.PlayerName} recommended {_targetPlayer.PlayerName} for good RP");

        Close();
    }

    public override void Close()
    {
        _token.Close();
    }
}
