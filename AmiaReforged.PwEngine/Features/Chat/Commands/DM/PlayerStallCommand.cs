using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Nui;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.DM;

/// <summary>
/// DM command to manage player stalls.
/// Usage: ./playerstall [view/suspend] [stall_tag] [buyer/seller]
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PlayerStallCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly WindowDirector _windowDirector;
    private readonly PlayerStallEventManager _eventManager;
    private readonly RuntimeCharacterService _characters;
    private readonly ICommandDispatcher _commandDispatcher;

    public PlayerStallCommand(
        IPlayerShopRepository shops,
        WindowDirector windowDirector,
        PlayerStallEventManager eventManager,
        RuntimeCharacterService characters,
        ICommandDispatcher commandDispatcher)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _windowDirector = windowDirector ?? throw new ArgumentNullException(nameof(windowDirector));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
    }

    public string Command => "./playerstall";
    public string Description => "Manage player stalls (list/view/suspend)";
    public string AllowedRoles => "DM";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM)
        {
            caller.SendServerMessage("This command is only available to DMs.", ColorConstants.Red);
            return;
        }

        if (args.Length < 1)
        {
            caller.SendServerMessage("Usage: ./playerstall [list/view/suspend] [stall_tag] [buyer/seller (for view)]", ColorConstants.Orange);
            return;
        }

        string action = args[0].ToLowerInvariant();

        // Handle list command separately as it doesn't need a stall tag
        if (action == "list")
        {
            HandleListCommand(caller);
            return;
        }

        // All other commands require a stall tag
        if (args.Length < 2)
        {
            caller.SendServerMessage("Usage: ./playerstall [view/suspend] [stall_tag] [buyer/seller (for view)]", ColorConstants.Orange);
            return;
        }

        string stallTag = args[1];

        // Find the stall by tag
        List<PlayerStall> stalls = _shops.ShopsByTag(stallTag);
        if (stalls.Count == 0)
        {
            caller.SendServerMessage($"No stall found with tag: {stallTag}", ColorConstants.Red);
            return;
        }

        if (stalls.Count > 1)
        {
            caller.SendServerMessage($"Multiple stalls found with tag: {stallTag}. Please be more specific.", ColorConstants.Red);
            return;
        }

        PlayerStall stall = stalls[0];

        switch (action)
        {
            case "view":
                await HandleViewCommand(caller, stall, args);
                break;

            case "suspend":
                await HandleSuspendCommand(caller, stall);
                break;

            default:
                caller.SendServerMessage($"Unknown action: {action}. Use 'list', 'view', or 'suspend'.", ColorConstants.Orange);
                break;
        }
    }

    private void HandleListCommand(NwPlayer caller)
    {
        List<PlayerStall> allStalls = _shops.AllShops();

        if (allStalls.Count == 0)
        {
            caller.SendServerMessage("No player stalls found in the database.", ColorConstants.Orange);
            return;
        }

        caller.SendServerMessage($"=== Player Stalls ({allStalls.Count} total) ===", ColorConstants.Cyan);

        foreach (PlayerStall stall in allStalls.OrderBy(s => s.AreaResRef).ThenBy(s => s.Tag))
        {
            string ownerInfo = stall.OwnerCharacterId.HasValue
                ? $"Owner: {stall.OwnerDisplayName ?? "Unknown"}"
                : "Unclaimed";

            string statusInfo = stall.SuspendedUtc.HasValue
                ? $" [SUSPENDED since {stall.SuspendedUtc.Value:yyyy-MM-dd}]"
                : stall.IsActive ? "" : " [INACTIVE]";

            string settlementInfo = !string.IsNullOrWhiteSpace(stall.SettlementTag)
                ? $" in {stall.SettlementTag}"
                : "";

            caller.SendServerMessage(
                $"  {stall.Tag} ({stall.AreaResRef}{settlementInfo}) - {ownerInfo}{statusInfo}",
                ColorConstants.White);
        }

        caller.SendServerMessage("Use './playerstall view [tag] [buyer/seller]' to inspect a stall.", ColorConstants.Gray);
        caller.SendServerMessage("Use './playerstall suspend [tag]' to suspend a stall.", ColorConstants.Gray);
    }

    private async Task HandleViewCommand(NwPlayer caller, PlayerStall stall, string[] args)
    {
        if (args.Length < 3)
        {
            caller.SendServerMessage("Usage: ./playerstall view [stall_tag] [buyer/seller]", ColorConstants.Orange);
            return;
        }

        string viewType = args[2].ToLowerInvariant();

        // Get persona for the DM
        if (!_characters.TryGetPlayerKey(caller, out Guid key) || key == Guid.Empty)
        {
            caller.SendServerMessage("Failed to resolve your character key.", ColorConstants.Red);
            return;
        }

        PersonaId personaId;
        try
        {
            CharacterId characterId = CharacterId.From(key);
            personaId = PersonaId.FromCharacter(characterId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create PersonaId for DM {PlayerName}", caller.PlayerName);
            caller.SendServerMessage("Failed to resolve your persona.", ColorConstants.Red);
            return;
        }

        switch (viewType)
        {
            case "buyer":
                await OpenBuyerWindowAsync(caller, stall, personaId);
                break;

            case "seller":
                await OpenSellerWindowAsync(caller, stall, personaId);
                break;

            default:
                caller.SendServerMessage($"Unknown view type: {viewType}. Use 'buyer' or 'seller'.", ColorConstants.Orange);
                break;
        }
    }

    private async Task HandleSuspendCommand(NwPlayer caller, PlayerStall stall)
    {
        if (stall.OwnerCharacterId == null)
        {
            caller.SendServerMessage($"Stall '{stall.Tag}' is not currently owned.", ColorConstants.Orange);
            return;
        }

        try
        {
            // Create the suspension command
            SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
                stallId: stall.Id,
                reason: $"Administrative suspension by DM {caller.PlayerName}",
                timestamp: DateTime.UtcNow,
                gracePeriod: TimeSpan.FromHours(24)); // Give a 24-hour grace period

            // Dispatch the command
            CommandResult result = await _commandDispatcher.DispatchAsync(command);

            if (result.Success)
            {
                caller.SendServerMessage($"Stall '{stall.Tag}' has been suspended. Items will be moved to the Market Reeve.", ColorConstants.Lime);
                Log.Info($"DM {caller.PlayerName} suspended stall {stall.Id} (Tag: {stall.Tag})");
            }
            else
            {
                caller.SendServerMessage($"Failed to suspend stall: {result.ErrorMessage ?? "Unknown error"}", ColorConstants.Red);
                Log.Error($"Failed to suspend stall {stall.Id} via DM command: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error suspending stall {stall.Id} via DM command");
            caller.SendServerMessage($"An error occurred while suspending the stall: {ex.Message}", ColorConstants.Red);
        }
    }

    private async Task OpenBuyerWindowAsync(NwPlayer player, PlayerStall stall, PersonaId personaId)
    {
        PlayerStallBuyerSnapshot? snapshot = await _eventManager
            .BuildBuyerSnapshotForAsync(stall.Id, personaId, player)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            Log.Warn($"Failed to build buyer snapshot for stall {stall.Id} while opening via DM command.");
            player.SendServerMessage("Failed to build buyer snapshot. Please try again.", ColorConstants.Red);
            return;
        }

        string title = string.IsNullOrWhiteSpace(snapshot.Summary.StallName)
            ? "Market Stall"
            : snapshot.Summary.StallName;

        PlayerStallBuyerWindowConfig config = new(
            stall.Id,
            personaId,
            title,
            snapshot);

        PlayerBuyerView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(PlayerBuyerPresenter));
        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage($"Opening buyer view for stall: {stall.Tag}", ColorConstants.Cyan);
    }

    private async Task OpenSellerWindowAsync(NwPlayer player, PlayerStall stall, PersonaId personaId)
    {
        PlayerStallSellerSnapshot? snapshot = await _eventManager
            .BuildSellerSnapshotForAsync(stall.Id, personaId)
            .ConfigureAwait(false);

        if (snapshot is null)
        {
            Log.Warn($"Failed to build seller snapshot for stall {stall.Id} while opening via DM command.");
            player.SendServerMessage("Failed to build seller snapshot. Please try again.", ColorConstants.Red);
            return;
        }

        string title = string.IsNullOrWhiteSpace(snapshot.Summary.StallName)
            ? "Stall Management"
            : snapshot.Summary.StallName;

        PlayerStallSellerWindowConfig config = new(
            stall.Id,
            personaId,
            title,
            snapshot);

        PlayerSellerView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(PlayerSellerPresenter));
        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage($"Opening seller view for stall: {stall.Tag}", ColorConstants.Cyan);
    }
}

