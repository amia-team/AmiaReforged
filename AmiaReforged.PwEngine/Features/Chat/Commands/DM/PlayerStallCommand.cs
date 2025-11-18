using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
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
    private readonly IPlayerStallInventoryCustodian _inventoryCustodian;
    private readonly IEventBus _eventBus;
    private readonly IReeveFundsService _reeveFunds;

    public PlayerStallCommand(
        IPlayerShopRepository shops,
        WindowDirector windowDirector,
        PlayerStallEventManager eventManager,
        RuntimeCharacterService characters,
        ICommandDispatcher commandDispatcher,
        IPlayerStallInventoryCustodian inventoryCustodian,
        IEventBus eventBus,
        IReeveFundsService reeveFunds)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _windowDirector = windowDirector ?? throw new ArgumentNullException(nameof(windowDirector));
        _eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _inventoryCustodian = inventoryCustodian ?? throw new ArgumentNullException(nameof(inventoryCustodian));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _reeveFunds = reeveFunds ?? throw new ArgumentNullException(nameof(reeveFunds));
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
            // Capture ownership info and escrow BEFORE clearing
            Guid? formerOwnerId = stall.OwnerCharacterId;
            string? formerPersonaId = stall.OwnerPersonaId;
            string formerOwnerName = stall.OwnerDisplayName ?? "Unknown";
            int escrowBalance = stall.EscrowBalance;  // Capture escrow BEFORE clearing
            string? areaResRef = stall.AreaResRef;     // Capture area BEFORE clearing
            DateTime now = DateTime.UtcNow;

            // Check if stall has items before transfer
            List<StallProduct>? products = _shops.ProductsForShop(stall.Id);
            int productCount = products?.Count ?? 0;

            Log.Info($"DM suspension: Stall {stall.Id} has {productCount} products and {escrowBalance} gp escrow. Owner: {stall.OwnerPersonaId ?? "null"}");

            if (productCount > 0)
            {
                caller.SendServerMessage($"Stall has {productCount} product listing(s). Transferring to Market Reeve...", ColorConstants.Cyan);
            }

            if (escrowBalance > 0)
            {
                caller.SendServerMessage($"Stall has {escrowBalance} gp in escrow. Transferring to Market Reeve vault...", ColorConstants.Cyan);
            }

            // Transfer inventory to market reeve custody FIRST (off main thread is OK)
            bool transferSuccess = false;
            string? transferError = null;

            try
            {
                await _inventoryCustodian.TransferInventoryToMarketReeveAsync(stall, CancellationToken.None)
                    .ConfigureAwait(false);
                transferSuccess = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to transfer inventory for stall {StallId} during DM suspension", stall.Id);
                transferError = ex.Message;
            }

            // Deposit escrow to vault BEFORE clearing ownership
            bool escrowSuccess = false;
            string? escrowError = null;

            if (escrowBalance > 0 && !string.IsNullOrWhiteSpace(formerPersonaId))
            {
                try
                {
                    PersonaId persona = PersonaId.Parse(formerPersonaId);
                    CommandResult vaultResult = await _reeveFunds.DepositHeldFundsAsync(
                        persona,
                        areaResRef,
                        escrowBalance,
                        $"Stall {stall.Id} DM suspension escrow",
                        CancellationToken.None).ConfigureAwait(false);

                    if (vaultResult.Success)
                    {
                        Log.Info("Deposited {Amount} gp escrow to vault for DM-suspended stall {StallId} owner {Persona}",
                            escrowBalance, stall.Id, persona);
                        escrowSuccess = true;
                    }
                    else
                    {
                        Log.Warn("Failed to deposit escrow to vault for stall {StallId}: {Error}",
                            stall.Id, vaultResult.ErrorMessage);
                        escrowError = vaultResult.ErrorMessage;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error depositing escrow to vault for stall {StallId}", stall.Id);
                    escrowError = ex.Message;
                }
            }

            // Clear ownership and deactivate stall
            bool updated = _shops.UpdateShop(stall.Id, entity =>
            {
                entity.OwnerCharacterId = null;
                entity.OwnerPersonaId = null;
                entity.OwnerPlayerPersonaId = null;
                entity.OwnerDisplayName = null;
                entity.CoinHouseAccountId = null;
                entity.HoldEarningsInStall = false;
                entity.EscrowBalance = 0;  // Zero out escrow (already deposited to vault)
                entity.CurrentTenureGrossSales = 0;
                entity.CurrentTenureNetEarnings = 0;
                entity.SuspendedUtc = now;
                entity.DeactivatedUtc = now;
                entity.IsActive = false;
                entity.NextRentDueUtc = now + TimeSpan.FromHours(1);
            });

            if (!updated)
            {
                await NwTask.SwitchToMainThread();
                caller.SendServerMessage($"Failed to update stall '{stall.Tag}' in database.", ColorConstants.Red);
                Log.Error($"Failed to update stall {stall.Id} during DM suspension");
                return;
            }

            // Publish ownership released event
            StallOwnershipReleasedEvent releaseEvent = new StallOwnershipReleasedEvent
            {
                StallId = stall.Id,
                FormerOwnerId = formerOwnerId,
                FormerPersonaId = formerPersonaId,
                Reason = $"Administrative suspension by DM {caller.PlayerName}"
            };

            await _eventBus.PublishAsync(releaseEvent, CancellationToken.None).ConfigureAwait(false);

            // Notify UI to refresh
            await _eventManager.BroadcastSellerRefreshAsync(stall.Id).ConfigureAwait(false);

            // Switch to main thread for NWN operations
            await NwTask.SwitchToMainThread();

            if (transferSuccess)
            {
                caller.SendServerMessage($"Stall inventory has been transferred to the Market Reeve.", ColorConstants.Lime);
            }
            else if (transferError != null)
            {
                caller.SendServerMessage($"Warning: Failed to transfer some inventory items: {transferError}", ColorConstants.Yellow);
            }

            if (escrowBalance > 0)
            {
                if (escrowSuccess)
                {
                    caller.SendServerMessage($"Escrow balance of {escrowBalance} gp has been transferred to the Market Reeve vault.", ColorConstants.Lime);
                }
                else if (escrowError != null)
                {
                    caller.SendServerMessage($"Warning: Failed to transfer escrow to vault: {escrowError}", ColorConstants.Yellow);
                }
            }

            caller.SendServerMessage($"Stall '{stall.Tag}' has been suspended and {formerOwnerName} has been evicted.", ColorConstants.Lime);
            Log.Info($"DM {caller.PlayerName} suspended stall {stall.Id} (Tag: {stall.Tag}), evicted owner {formerOwnerName}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error suspending stall {stall.Id} via DM command");

            await NwTask.SwitchToMainThread();
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
