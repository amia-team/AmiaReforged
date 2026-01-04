using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking;

/// <summary>
/// Bank storage window for personal item storage.
/// </summary>
public sealed class BankStorageWindowView : ScryView<BankStorageWindowPresenter>
{
    // Window dimensions
    public const float WindowWidth = 720f;
    public const float WindowHeight = 600f;
    public const float WindowPosX = 140f;
    public const float WindowPosY = 140f;

    // Calculated dimensions
    private const float ContentWidth = WindowWidth - 40f;
    private const float HalfWidth = (ContentWidth - 12f) / 2f;
    private const float StandardButtonWidth = 100f;
    private const float StandardButtonHeight = 32f;
    private const float StandardSpacing = 8f;

    // Bindings
    public readonly NuiBind<string> StorageCapacityText = new("storage_capacity");
    public readonly NuiBind<int> StorageItemCount = new("storage_item_count");
    public readonly NuiBind<string> StorageItemLabels = new("storage_item_labels");
    public readonly NuiBind<string> StorageItemTooltips = new("storage_item_tooltips");
    public readonly NuiBind<bool> CanUpgradeStorage = new("can_upgrade_storage");
    public readonly NuiBind<string> UpgradeCostText = new("upgrade_cost");
    public readonly NuiBind<int> InventoryItemCount = new("inventory_item_count");
    public readonly NuiBind<string> InventoryItemLabels = new("inventory_item_labels");
    public readonly NuiBind<string> InventoryItemTooltips = new("inventory_item_tooltips");

    public override BankStorageWindowPresenter Presenter { get; protected set; }

    public BankStorageWindowView(NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        Presenter = new BankStorageWindowPresenter(this, player, coinhouseTag, bankDisplayName);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> storedItemRowTemplate =
        [
            new(new NuiButton(StorageItemLabels)
            {
                Id = "storage_item_withdraw",
                Height = 26f,
                Tooltip = StorageItemTooltips
            })
        ];

        List<NuiListTemplateCell> inventoryRowTemplate =
        [
            new(new NuiButton(InventoryItemLabels)
            {
                Id = "storage_item_store",
                Height = 26f,
                Tooltip = InventoryItemTooltips
            })
        ];

        const float storageListHeight = 360f;
        const float labelHeight = 22f;
        const float actualListHeight = storageListHeight - labelHeight - 8f;

        NuiColumn root = new()
        {
            Children =
            [
                // Header with capacity and upgrade
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel(StorageCapacityText)
                        {
                            Width = ContentWidth - StandardButtonWidth - 20f,
                            Height = 28f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer(),
                        new NuiButton("Upgrade")
                        {
                            Id = "storage_btn_upgrade",
                            Width = StandardButtonWidth,
                            Height = 26f,
                            Visible = CanUpgradeStorage
                        }
                    ]
                },
                new NuiLabel(UpgradeCostText)
                {
                    Height = 20f,
                    Visible = CanUpgradeStorage,
                    HorizontalAlign = NuiHAlign.Left
                },
                new NuiSpacer { Height = StandardSpacing },
                // Two-column layout: Inventory | Storage
                new NuiRow
                {
                    Children =
                    [
                        new NuiColumn
                        {
                            Children =
                            [
                                new NuiLabel("Your Inventory (click to store)")
                                {
                                    Height = labelHeight,
                                    Width = HalfWidth,
                                    HorizontalAlign = NuiHAlign.Left
                                },
                                new NuiList(inventoryRowTemplate, InventoryItemCount)
                                {
                                    RowHeight = 28f,
                                    Height = actualListHeight,
                                    Width = HalfWidth
                                }
                            ]
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiColumn
                        {
                            Children =
                            [
                                new NuiLabel("Stored Items (click to withdraw)")
                                {
                                    Height = labelHeight,
                                    Width = HalfWidth,
                                    HorizontalAlign = NuiHAlign.Left
                                },
                                new NuiList(storedItemRowTemplate, StorageItemCount)
                                {
                                    RowHeight = 28f,
                                    Height = actualListHeight,
                                    Width = HalfWidth
                                }
                            ]
                        }
                    ]
                },
                new NuiSpacer(),
                // Footer
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "storage_btn_close",
                            Width = 110f,
                            Height = StandardButtonHeight
                        }
                    ]
                }
            ]
        };

        return root;
    }
}

public sealed class BankStorageWindowPresenter : ScryPresenter<BankStorageWindowView>
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly CoinhouseTag _coinhouseTag;
    private readonly string _bankDisplayName;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private List<StoredItemDto> _storedItems = [];
    private List<NwItem> _inventoryItems = [];
    private int _storageCapacity = 10;

    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;
    [Inject] private Lazy<IWorldEngineFacade> WorldEngine { get; init; } = null!;
    [Inject] private Lazy<IBankStorageItemBlacklist> StorageBlacklist { get; init; } = null!;

    public BankStorageWindowPresenter(BankStorageWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _coinhouseTag = coinhouseTag;
        _bankDisplayName = bankDisplayName;
    }

    public override BankStorageWindowView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), $"{_bankDisplayName} - Storage")
        {
            Geometry = new NuiRect(BankStorageWindowView.WindowPosX, BankStorageWindowView.WindowPosY,
                BankStorageWindowView.WindowWidth, BankStorageWindowView.WindowHeight),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null) InitBefore();
        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        _ = LoadStorageDataAsync();
    }

    public override void Close()
    {
        _token.Close();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        Log.Info($"Storage window event: Type={obj.EventType}, ElementId={obj.ElementId}, ArrayIndex={obj.ArrayIndex}");

        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case "storage_btn_upgrade":
                Log.Info("Upgrade button clicked");
                _ = HandleUpgradeStorageAsync();
                break;
            case "storage_btn_close":
                Log.Info("Close button clicked");
                Close();
                break;
            case "storage_item_store":
                if (obj.ArrayIndex >= 0)
                {
                    Log.Info($"Store item clicked: index={obj.ArrayIndex}");
                    _ = HandleStoreItemAsync(obj.ArrayIndex);
                }
                break;
            case "storage_item_withdraw":
                if (obj.ArrayIndex >= 0)
                {
                    Log.Info($"Withdraw item clicked: index={obj.ArrayIndex}");
                    _ = HandleWithdrawItemAsync(obj.ArrayIndex);
                }
                break;
        }
    }

    private async Task LoadStorageDataAsync()
    {
        try
        {
            Guid characterId = CharacterService.Value.GetPlayerKey(_player);
            if (characterId == Guid.Empty) return;

            // Load stored items and capacity using facade service
            _storedItems = await WorldEngine.Value.Economy.Storage.GetStoredItemsAsync(_coinhouseTag, characterId, CancellationToken.None);

            GetStorageCapacityResult capacityInfo = await WorldEngine.Value.Economy.Storage.GetStorageCapacityAsync(_coinhouseTag, characterId, CancellationToken.None);
            _storageCapacity = capacityInfo.TotalCapacity;

            await NwTask.SwitchToMainThread();

            Token().SetBindValue(View.StorageCapacityText, $"Storage: {_storedItems.Count} / {_storageCapacity} slots used");
            Token().SetBindValue(View.StorageItemCount, _storedItems.Count);
            Token().SetBindValues(View.StorageItemLabels, _storedItems.Select(i => i.Name ?? "Unknown Item").ToList());
            Token().SetBindValues(View.StorageItemTooltips, _storedItems.Select(i => i.Description ?? "No description").ToList());

            // Check if can upgrade
            bool canUpgrade = capacityInfo.CanUpgrade;
            Token().SetBindValue(View.CanUpgradeStorage, canUpgrade);

            if (canUpgrade)
            {
                Token().SetBindValue(View.UpgradeCostText, $"Upgrade to {_storageCapacity + 10} slots: {capacityInfo.NextUpgradeCost:N0} gp");
            }

            // Load inventory items
            if (_player.ControlledCreature != null)
            {
                List<NwItem> inventoryItems = _player.ControlledCreature.Inventory.Items
                    .Where(i => i.IsValid && !string.IsNullOrEmpty(i.Name))
                    .ToList();

                IBankStorageItemBlacklist blacklist = StorageBlacklist.Value;
                _inventoryItems = inventoryItems
                    .Where(i => !blacklist.IsBlockedFromStorage(i))
                    .ToList();

                int filteredCount = inventoryItems.Count - _inventoryItems.Count;
                if (filteredCount > 0)
                {
                    Log.Debug("Filtered {Filtered} inventory items from bank storage view for character {CharacterId} at bank {Bank}",
                        filteredCount,
                        characterId,
                        _coinhouseTag.Value);
                }

                Token().SetBindValue(View.InventoryItemCount, _inventoryItems.Count);
                Token().SetBindValues(View.InventoryItemLabels, _inventoryItems.Select(i => i.Name).ToList());
                Token().SetBindValues(View.InventoryItemTooltips, _inventoryItems.Select(i => i.Description ?? "No description").ToList());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading storage data for {Player} at {Bank}", _player.PlayerName, _bankDisplayName);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage("Failed to load storage data.", ColorConstants.Orange);
        }
    }

    private async Task HandleUpgradeStorageAsync()
    {
        try
        {
            Guid characterId = CharacterService.Value.GetPlayerKey(_player);
            if (characterId == Guid.Empty) return;

            // Get current capacity
            GetStorageCapacityResult capacityInfo = await WorldEngine.Value.Economy.Storage.GetStorageCapacityAsync(_coinhouseTag, characterId, CancellationToken.None);

            if (!capacityInfo.CanUpgrade)
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage("Your storage is already at maximum capacity.", ColorConstants.White);
                return;
            }

            int cost = capacityInfo.NextUpgradeCost ?? 0;

            await NwTask.SwitchToMainThread();
            NwCreature? controlledCreature = _player.ControlledCreature;

            if (controlledCreature == null)
            {
                Token().Player.SendServerMessage("You must possess your character to upgrade storage.", ColorConstants.Orange);
                return;
            }

            uint playerGold = controlledCreature.Gold;

            if (playerGold < cost)
            {
                Token().Player.SendServerMessage($"You need {cost:N0} gp to upgrade storage. You have {playerGold:N0} gp.", ColorConstants.Orange);
                return;
            }

            uint deduction = (uint)cost;
            controlledCreature.Gold -= deduction;

            // Upgrade storage using facade service
            CommandResult result = await WorldEngine.Value.Economy.Storage.UpgradeStorageCapacityAsync(_coinhouseTag, characterId, CancellationToken.None);

            if (result.Success)
            {
                int newCapacity = (int)result.Data["NewCapacity"];
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage($"Storage upgraded! New capacity: {newCapacity} slots.", ColorConstants.Green);
                // Reload data
                await LoadStorageDataAsync();
            }
            else
            {
                await NwTask.SwitchToMainThread();

                if (controlledCreature.IsValid)
                {
                    controlledCreature.Gold += deduction;
                }

                Token().Player.SendServerMessage(result.ErrorMessage ?? "Upgrade failed", ColorConstants.Orange);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error upgrading storage for {Player} at {Bank}", _player.PlayerName, _bankDisplayName);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage("Failed to upgrade storage.", ColorConstants.Orange);
        }
    }

    private async Task HandleStoreItemAsync(int itemIndex)
    {
        try
        {
            if (itemIndex < 0 || itemIndex >= _inventoryItems.Count) return;

            Guid characterId = CharacterService.Value.GetPlayerKey(_player);
            if (characterId == Guid.Empty) return;

            NwItem item = _inventoryItems[itemIndex];
            if (!item.IsValid)
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage("Item is no longer valid.", ColorConstants.Orange);
                await LoadStorageDataAsync();
                return;
            }

            if (StorageBlacklist.Value.IsBlockedFromStorage(item))
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage("That item cannot be stored in the bank.", ColorConstants.Orange);
                await LoadStorageDataAsync();
                return;
            }

            // Serialize item
            byte[] itemData = item.Serialize();
            string itemName = item.Name ?? "Unknown Item";
            string itemDescription = item.Description ?? "";

            // Store item using facade service
            CommandResult result = await WorldEngine.Value.Economy.Storage.StoreItemAsync(_coinhouseTag, characterId, itemName, itemDescription, itemData, CancellationToken.None);

            await NwTask.SwitchToMainThread();

            if (result.Success)
            {
                // Destroy original item
                item.Destroy();
                string message = result.Data.ContainsKey("Message") ? (string)result.Data["Message"] : $"Stored: {itemName}";
                Token().Player.SendServerMessage(message, ColorConstants.Green);
                // Reload data
                await LoadStorageDataAsync();
            }
            else
            {
                Token().Player.SendServerMessage(result.ErrorMessage ?? "Failed to store item", ColorConstants.Orange);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error storing item for {Player} at {Bank}", _player.PlayerName, _bankDisplayName);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage("Failed to store item.", ColorConstants.Orange);
        }
    }

    private async Task HandleWithdrawItemAsync(int itemIndex)
    {
        try
        {
            if (itemIndex < 0 || itemIndex >= _storedItems.Count) return;

            Guid characterId = CharacterService.Value.GetPlayerKey(_player);
            if (characterId == Guid.Empty) return;

            StoredItemDto storedItem = _storedItems[itemIndex];

            // Withdraw item using facade service
            CommandResult result = await WorldEngine.Value.Economy.Storage.WithdrawItemAsync(storedItem.ItemId, characterId, CancellationToken.None);

            await NwTask.SwitchToMainThread();

            if (result.Success && result.Data.ContainsKey("ItemData"))
            {
                byte[] itemData = (byte[])result.Data["ItemData"];
                string itemName = (string)result.Data["ItemName"];

                // Deserialize and create item in player's inventory
                NwItem? deserializedItem = NwItem.Deserialize(itemData);

                if (deserializedItem != null && _player.ControlledCreature != null)
                {
                    _player.ControlledCreature.AcquireItem(deserializedItem);
                    Token().Player.SendServerMessage($"Withdrew: {itemName}", ColorConstants.Green);
                }
                else
                {
                    Token().Player.SendServerMessage("Failed to create item from storage data.", ColorConstants.Orange);
                }

                // Reload data
                await LoadStorageDataAsync();
            }
            else
            {
                Token().Player.SendServerMessage(result.ErrorMessage ?? "Failed to withdraw item", ColorConstants.Orange);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error withdrawing item for {Player} at {Bank}", _player.PlayerName, _bankDisplayName);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage("Failed to withdraw item.", ColorConstants.Orange);
        }
    }
}
