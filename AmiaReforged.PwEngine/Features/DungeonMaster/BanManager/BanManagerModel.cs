using AmiaReforged.Core.Models;
using AmiaReforged.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.BanManager;

/// <summary>
/// Model for managing bans in the DM tool window.
/// </summary>
public sealed class BanManagerModel
{
    [Inject] private Lazy<BanService> BanService { get; init; } = null!;

    private readonly NwPlayer _dmPlayer;
    private string _searchTerm = string.Empty;

    public List<Ban> AllBans { get; private set; } = [];
    public List<Ban> VisibleBans { get; private set; } = [];

    public delegate void BanListUpdateEventHandler(BanManagerModel sender, EventArgs e);
    public event BanListUpdateEventHandler? OnBansUpdated;

    public BanManagerModel(NwPlayer dmPlayer)
    {
        _dmPlayer = dmPlayer;
    }

    public async Task LoadBansAsync()
    {
        AllBans = await BanService.Value.GetAllBansAsync();
        RefreshVisibleBans();
    }

    public void SetSearchTerm(string search)
    {
        _searchTerm = search;
        RefreshVisibleBans();
    }

    public void RefreshVisibleBans()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            VisibleBans = AllBans.ToList();
        }
        else
        {
            VisibleBans = AllBans
                .Where(b => b.CdKey.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        OnBansUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> BanCdKeyAsync(string cdKey)
    {
        if (string.IsNullOrWhiteSpace(cdKey))
        {
            _dmPlayer.SendServerMessage("CD Key is required.", ColorConstants.Red);
            return false;
        }

        string trimmedCdKey = cdKey.Trim();

        bool success = await BanService.Value.BanAsync(trimmedCdKey);
        await NwTask.SwitchToMainThread();

        if (success)
        {
            _dmPlayer.SendServerMessage($"CD Key {trimmedCdKey} has been banned.", ColorConstants.Lime);

            // Boot the player if they're online
            BootPlayerIfOnline(trimmedCdKey);

            await LoadBansAsync();
            return true;
        }
        else
        {
            _dmPlayer.SendServerMessage($"Failed to ban {trimmedCdKey}. May already be banned.", ColorConstants.Red);
            return false;
        }
    }

    public async Task<bool> UnbanCdKeyAsync(string cdKey)
    {
        bool success = await BanService.Value.UnbanAsync(cdKey);
        await NwTask.SwitchToMainThread();

        if (success)
        {
            _dmPlayer.SendServerMessage($"CD Key {cdKey} has been unbanned.", ColorConstants.Lime);
            await LoadBansAsync();
            return true;
        }
        else
        {
            _dmPlayer.SendServerMessage($"Failed to unban {cdKey}.", ColorConstants.Red);
            return false;
        }
    }

    public void EnterTargetingMode(Action<string> onCdKeySelected)
    {
        _dmPlayer.EnterTargetMode(ev => OnTargetSelected(ev, onCdKeySelected), new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void OnTargetSelected(ModuleEvents.OnPlayerTarget ev, Action<string> onCdKeySelected)
    {
        if (ev.TargetObject is not NwCreature creature)
        {
            _dmPlayer.SendServerMessage("Invalid target. Please select a creature.", ColorConstants.Red);
            return;
        }

        if (!creature.IsPlayerControlled(out NwPlayer? targetPlayer))
        {
            _dmPlayer.SendServerMessage("Target must be a player character.", ColorConstants.Red);
            return;
        }

        string cdKey = targetPlayer.CDKey;
        _dmPlayer.SendServerMessage($"Selected player: {targetPlayer.PlayerName} (CD Key: {cdKey})", ColorConstants.White);
        onCdKeySelected(cdKey);
    }

    private void BootPlayerIfOnline(string cdKey)
    {
        NwPlayer? player = NwModule.Instance.Players
            .FirstOrDefault(p => p.IsValid && p.CDKey == cdKey);

        if (player != null)
        {
            player.BootPlayer("You have been banned.");
            _dmPlayer.SendServerMessage($"Player {player.PlayerName} has been booted.", ColorConstants.Orange);
        }
    }
}
