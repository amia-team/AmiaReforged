using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

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
    public readonly NuiBind<bool> CanUpgradeStorage = new("can_upgrade_storage");
    public readonly NuiBind<string> UpgradeCostText = new("upgrade_cost");
    public readonly NuiBind<int> InventoryItemCount = new("inventory_item_count");
    public readonly NuiBind<string> InventoryItemLabels = new("inventory_item_labels");
    
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
            new(new NuiLabel(StorageItemLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        List<NuiListTemplateCell> inventoryRowTemplate =
        [
            new(new NuiLabel(InventoryItemLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
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
                    Height = 28f,
                    Children =
                    [
                        new NuiLabel(StorageCapacityText)
                        {
                            Width = ContentWidth - StandardButtonWidth - 20f,
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
                    Height = storageListHeight,
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
                    Height = 36f,
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
    
    private List<StoredItem> _storedItems = [];
    private List<NwItem> _inventoryItems = [];
    private int _storageCapacity = 10;

    [Inject] private Lazy<IPersonalStorageService> PersonalStorageService { get; init; } = null!;
    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;

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
            Resizable = false
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
        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case "storage_btn_upgrade":
                _ = HandleUpgradeStorageAsync();
                break;
            case "storage_btn_close":
                Close();
                break;
        }
    }

    private async Task LoadStorageDataAsync()
    {
        try
        {
            Guid characterId = CharacterService.Value.GetPlayerKey(_player);
            if (characterId == Guid.Empty) return;
            
            // Load stored items
            _storedItems = await PersonalStorageService.Value.GetStoredItemsAsync(_coinhouseTag, characterId, CancellationToken.None);
            var capacityInfo = await PersonalStorageService.Value.GetStorageCapacityAsync(_coinhouseTag, characterId, CancellationToken.None);
            _storageCapacity = capacityInfo.Capacity;
            
            await NwTask.SwitchToMainThread();
            
            Token().SetBindValue(View.StorageCapacityText, $"Storage: {_storedItems.Count} / {_storageCapacity} slots used");
            Token().SetBindValue(View.StorageItemCount, _storedItems.Count);
            Token().SetBindValue(View.StorageItemLabels, string.Join("\n", _storedItems.Select(i => i.Name ?? "Unknown Item")));
            
            // Check if can upgrade
            bool canUpgrade = _storageCapacity < 100;
            Token().SetBindValue(View.CanUpgradeStorage, canUpgrade);
            
            if (canUpgrade)
            {
                int cost = PersonalStorageService.Value.CalculateUpgradeCost(_storageCapacity);
                Token().SetBindValue(View.UpgradeCostText, $"Upgrade to {_storageCapacity + 10} slots: {cost:N0} gp");
            }
            
            // Load inventory items
            if (_player.ControlledCreature != null)
            {
                _inventoryItems = _player.ControlledCreature.Inventory.Items.Where(i => i.IsValid && !string.IsNullOrEmpty(i.Name)).ToList();
                Token().SetBindValue(View.InventoryItemCount, _inventoryItems.Count);
                Token().SetBindValue(View.InventoryItemLabels, string.Join("\n", _inventoryItems.Select(i => i.Name)));
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
            
            if (_storageCapacity >= 100)
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage("Your storage is already at maximum capacity.", ColorConstants.White);
                return;
            }
            
            int cost = PersonalStorageService.Value.CalculateUpgradeCost(_storageCapacity);
            uint playerGold = _player.ControlledCreature?.Gold ?? 0;
            
            if (playerGold < cost)
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage($"You need {cost:N0} gp to upgrade storage. You have {playerGold:N0} gp.", ColorConstants.Orange);
                return;
            }
            
            // Deduct gold
            await NwTask.WaitUntilValueChanged(() => _player.ControlledCreature);
            if (_player.ControlledCreature != null)
            {
                _player.ControlledCreature.Gold -= (uint)cost;
            }
            
            // Upgrade storage
            await PersonalStorageService.Value.UpgradeStorageCapacityAsync(_coinhouseTag, characterId, CancellationToken.None);
            
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage($"Storage upgraded! New capacity: {_storageCapacity + 10} slots.", ColorConstants.Green);
            
            // Reload data
            await LoadStorageDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error upgrading storage for {Player} at {Bank}", _player.PlayerName, _bankDisplayName);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage("Failed to upgrade storage.", ColorConstants.Orange);
        }
    }
}
