using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Access;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Front end for player bank interactions.
/// </summary>
public sealed class BankWindowView : ScryView<BankWindowPresenter>
{
    // Window dimensions - configure once, use everywhere
    public const float WindowWidth = 720f;
    public const float WindowHeight = 900f;
    public const float WindowPosX = 120f;
    public const float WindowPosY = 120f;

    // Calculated dimensions for consistent spacing
    private const float ContentWidth = WindowWidth - 40f; // Accounting for padding
    private const float HalfWidth = (ContentWidth - 12f) / 2f; // Split for two columns
    private const float TabButtonWidth = 120f;
    private const float StandardButtonWidth = 100f;
    private const float StandardButtonHeight = 32f;
    private const float StandardRowHeight = 36f;
    private const float StandardSpacing = 8f;

    public readonly NuiBind<string> BankTitle = new("bank_title");
    public readonly NuiBind<string> BankSubtitle = new("bank_subtitle");
    public readonly NuiBind<int> AccountEntryCount = new("bank_account_entry_count");
    public readonly NuiBind<string> AccountEntries = new("bank_account_entries");
    public readonly NuiBind<int> HoldingCount = new("bank_holding_count");
    public readonly NuiBind<string> HoldingEntries = new("bank_holding_entries");
    public readonly NuiBind<string> BalanceText = new("bank_balance_text");
    public readonly NuiBind<string> LastAccessed = new("bank_last_accessed");
    public readonly NuiBind<List<NuiComboEntry>> DepositModeEntries = new("bank_deposit_mode_entries");
    public readonly NuiBind<int> DepositModeSelection = new("bank_deposit_mode_selection");
    public readonly NuiBind<string> DepositAmountText = new("bank_deposit_amount_text");
    public readonly NuiBind<List<NuiComboEntry>> WithdrawModeEntries = new("bank_withdraw_mode_entries");
    public readonly NuiBind<int> WithdrawModeSelection = new("bank_withdraw_mode_selection");
    public readonly NuiBind<string> WithdrawAmountText = new("bank_withdraw_amount_text");
    public readonly NuiBind<int> InventoryItemCount = new("bank_inventory_item_count");
    public readonly NuiBind<string> InventoryItemLabels = new("bank_inventory_item_labels");
    public readonly NuiBind<int> PendingDepositCount = new("bank_pending_deposit_count");
    public readonly NuiBind<string> PendingDepositLabels = new("bank_pending_deposit_labels");
    public readonly NuiBind<string> EligibilitySummary = new("bank_eligibility_summary");
    public readonly NuiBind<string> PersonalEligibilityStatus = new("bank_personal_eligibility_status");
    public readonly NuiBind<string> OrganizationEligibilityStatus = new("bank_organization_eligibility_status");
    public readonly NuiBind<List<NuiComboEntry>> OrganizationAccountEntries = new("bank_org_account_entries");
    public readonly NuiBind<int> OrganizationAccountSelection = new("bank_org_account_selection");
    public readonly NuiBind<List<NuiComboEntry>> ShareTypeEntries = new("bank_share_type_entries");
    public readonly NuiBind<int> ShareTypeSelection = new("bank_share_type_selection");
    public readonly NuiBind<string> ShareInstructions = new("bank_share_instructions");

    public NuiButton DepositButton = null!;
    public NuiButton WithdrawButton = null!;
    public NuiButton ViewHistoryButton = null!;
    public NuiButton CloseAccountButton = null!;
    public NuiButton DoneButton = null!;
    public NuiButton CancelButton = null!;
    public NuiButton HelpButton = null!;
    public NuiButton OpenPersonalAccountButton = null!;
    public NuiButton OpenOrganizationAccountButton = null!;
    public NuiButton IssueShareDocumentButton = null!;
    public NuiButton ReclaimForeclosedItemsButton = null!;

    public NuiBind<bool> ShowPersonalAccountActions = new("bank_show_personal_actions");
    public NuiBind<bool> IsOrganizationLeader = new("bank_is_org_leader");
    public NuiBind<bool> ShowShareTools = new("bank_show_share_tools");
    public NuiBind<bool> HasForeclosedItems = new("bank_has_foreclosed_items");
    public NuiBind<int> ForeclosedItemCount = new("bank_foreclosed_item_count");
    public NuiBind<string> ForeclosedItemLabels = new("bank_foreclosed_item_labels");

    // Personal Storage bindings
    public NuiBind<string> PersonalStorageCapacityText = new("bank_personal_storage_capacity");
    public NuiBind<int> PersonalStorageItemCount = new("bank_personal_storage_item_count");
    public NuiBind<string> PersonalStorageItemLabels = new("bank_personal_storage_item_labels");
    public NuiBind<bool> CanUpgradeStorage = new("bank_can_upgrade_storage");
    public NuiBind<string> UpgradeStorageCostText = new("bank_upgrade_storage_cost");
    public NuiButton UpgradeStorageButton = null!;
    public NuiButton StoreItemButton = null!;
    public NuiButton WithdrawStoredItemButton = null!;

    // Window buttons
    public NuiButton OpenStorageWindowButton = null!;
    public NuiButton OpenAdminWindowButton = null!;

    public BankWindowView(NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        Presenter = new BankWindowPresenter(this, player, coinhouseTag, bankDisplayName);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override BankWindowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> accountRowTemplate =
        [
            new(new NuiLabel(AccountEntries)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        List<NuiListTemplateCell> holdingRowTemplate =
        [
            new(new NuiLabel(HoldingEntries)
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

        List<NuiListTemplateCell> pendingRowTemplate =
        [
            new(new NuiLabel(PendingDepositLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        List<NuiListTemplateCell> storedItemRowTemplate =
        [
            new(new NuiLabel(PersonalStorageItemLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        List<NuiListTemplateCell> foreclosedRowTemplate =
        [
            new(new NuiLabel(ForeclosedItemLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        NuiColumn root = new()
        {
            Children =
            [
                BuildHeader(),
                new NuiSpacer { Height = StandardSpacing },
                // Quick access buttons for other windows
                new NuiRow
                {
                    Height = StandardButtonHeight,
                    Children =
                    [
                        new NuiButton("Open Storage")
                        {
                            Id = "bank_btn_open_storage",
                            Width = TabButtonWidth + 20f,
                            Height = StandardButtonHeight - 4f
                        }.Assign(out OpenStorageWindowButton),
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Open Admin")
                        {
                            Id = "bank_btn_open_admin",
                            Width = TabButtonWidth,
                            Height = StandardButtonHeight - 4f
                        }.Assign(out OpenAdminWindowButton),
                        new NuiSpacer()
                    ]
                },
                new NuiSpacer { Height = StandardSpacing },
                // Banking content
                BuildBankingTab(accountRowTemplate, holdingRowTemplate, inventoryRowTemplate,
                    pendingRowTemplate),
                new NuiSpacer(),
                BuildFooterButtons()
            ]
        };

        return root;
    }

    private NuiElement BuildBankingTab(
        IReadOnlyList<NuiListTemplateCell> accountRowTemplate,
        IReadOnlyList<NuiListTemplateCell> holdingRowTemplate,
        IReadOnlyList<NuiListTemplateCell> inventoryRowTemplate,
        IReadOnlyList<NuiListTemplateCell> pendingRowTemplate)
    {
        return new NuiColumn
        {
            Children =
            [
                // Account opening section
                new NuiColumn
                {
                    Visible = ShowPersonalAccountActions,
                    Children =
                    [
                        new NuiLabel(EligibilitySummary)
                        {
                            Height = 22f,
                            HorizontalAlign = NuiHAlign.Left
                        },
                        new NuiRow
                        {
                            // Height = StandardRowHeight,
                            Children =
                            [
                                new NuiButton("Open Personal Account")
                                {
                                    Id = "bank_btn_open_personal",
                                    Width = 180f,
                                    Height = StandardButtonHeight
                                }.Assign(out OpenPersonalAccountButton),
                                new NuiSpacer { Width = 12f },
                                new NuiLabel(PersonalEligibilityStatus)
                                {
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle
                                }
                            ]
                        },
                        new NuiSpacer { Height = StandardSpacing - 2f }
                    ]
                },
                // Accounts and transactions
                new NuiLabel("Your Accounts")
                {
                    Height = 26f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                BuildMainContent(accountRowTemplate, holdingRowTemplate, pendingRowTemplate)
            ]
        };
    }

    private NuiElement BuildHeader()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiLabel(BankTitle)
                {
                    Height = 28f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(BankSubtitle)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                }
            ]
        };
    }

    private NuiElement BuildMainContent(
        IReadOnlyList<NuiListTemplateCell> accountRowTemplate,
        IReadOnlyList<NuiListTemplateCell> holdingRowTemplate,
        IReadOnlyList<NuiListTemplateCell> pendingRowTemplate)
    {
        NuiColumn leftColumn = new()
        {
            Children =
            [
                new NuiList(accountRowTemplate, AccountEntryCount)
                {
                    RowHeight = 32f,
                    Height = 110f,
                    Width = ContentWidth
                },
                new NuiSpacer { Height = 6f },
                new NuiList(holdingRowTemplate, HoldingCount)
                {
                    RowHeight = 30f,
                    Height = 100f,
                    Width = ContentWidth
                },
                new NuiSpacer { Height = 4f },
                new NuiLabel(BalanceText)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiLabel(LastAccessed)
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 8f },
                new NuiRow
                {
                    // Height = 36f,
                    Children =
                    [
                        new NuiLabel("Deposit")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiCombo
                        {
                            Id = "bank_deposit_combo",
                            Width = 170f,
                            Entries = DepositModeEntries,
                            Selected = DepositModeSelection
                        }
                    ]
                },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiLabel("Coin Amount")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiTextEdit("", DepositAmountText, 9, false)
                        {
                            Width = 140f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Deposit")
                        {
                            Id = "bank_btn_deposit",
                            Width = StandardButtonWidth,
                            Height = StandardButtonHeight
                        }.Assign(out DepositButton)
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiLabel("Withdraw")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiCombo
                        {
                            Id = "bank_withdraw_combo",
                            Width = 170f,
                            Entries = WithdrawModeEntries,
                            Selected = WithdrawModeSelection
                        }
                    ]
                },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiLabel("Coin Amount")
                        {
                            Width = 80f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiTextEdit("", WithdrawAmountText, 9, false)
                        {
                            Width = 140f
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiButton("Withdraw")
                        {
                            Id = "bank_btn_withdraw",
                            Width = StandardButtonWidth,
                            Height = StandardButtonHeight
                        }.Assign(out WithdrawButton)
                    ]
                },
                new NuiSpacer { Height = 8f },
                new NuiLabel("Account Sharing")
                {
                    Height = 22f,
                    Visible = ShowShareTools,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 36f,
                    Visible = ShowShareTools,
                    Children =
                    [
                        new NuiCombo
                        {
                            Id = "bank_share_combo",
                            Width = 170f,
                            Entries = ShareTypeEntries,
                            Selected = ShareTypeSelection
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton("Issue Share Document")
                        {
                            Id = "bank_btn_issue_share",
                            Width = 180f,
                            Height = 32f
                        }.Assign(out IssueShareDocumentButton)
                    ]
                },
                new NuiLabel(ShareInstructions)
                {
                    Visible = ShowShareTools,
                    Height = 40f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Top
                },
                new NuiSpacer { Height = 8f },
                new NuiLabel("Pending Transactions")
                {
                    Height = 22f
                },
                new NuiList(pendingRowTemplate, PendingDepositCount)
                {
                    RowHeight = 28f,
                    Height = 90f,
                    Width = ContentWidth
                }
            ]
        };

        return leftColumn;
    }

    private NuiElement BuildSecondaryActions()
    {
        List<NuiListTemplateCell> foreclosedRowTemplate =
        [
            new(new NuiLabel(ForeclosedItemLabels)
            {
                HorizontalAlign = NuiHAlign.Left,
                VerticalAlign = NuiVAlign.Middle
            })
        ];

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Height = 26f,
                    Visible = HasForeclosedItems,
                    Children =
                    [
                        new NuiLabel("Foreclosed Items Available")
                        {
                            Width = 260f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                new NuiRow
                {
                    Visible = HasForeclosedItems,
                    Children =
                    [
                        new NuiList(foreclosedRowTemplate, ForeclosedItemCount)
                        {
                            RowHeight = 28f,
                            Height = 120f,
                            Width = 400f
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton("Reclaim Item")
                        {
                            Id = "bank_btn_reclaim_foreclosed",
                            Width = 160f,
                            Height = 32f
                        }.Assign(out ReclaimForeclosedItemsButton)
                    ]
                },
                new NuiSpacer { Height = 6f },
                new NuiRow
                {
                    Height = 40f,
                    Children =
                    [
                        new NuiButton("View History")
                        {
                            Id = "bank_btn_view_history",
                            Width = 150f,
                            Height = 32f
                        }.Assign(out ViewHistoryButton),
                        new NuiSpacer(),
                        new NuiButton("Close Account")
                        {
                            Id = "bank_btn_close_account",
                            Width = 150f,
                            Height = 32f
                        }.Assign(out CloseAccountButton)
                    ]
                }
            ]
        };
    }

    private NuiElement BuildPersonalStorageSection()
    {
        List<NuiListTemplateCell> storedItemRowTemplate =
        [
            new(new NuiLabel(PersonalStorageItemLabels)
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

        return new NuiColumn
        {
            Children =
            [
                new NuiLabel("Personal Storage")
                {
                    Height = 26f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 28f,
                    Children =
                    [
                        new NuiLabel(PersonalStorageCapacityText)
                        {
                            Width = 420f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSpacer(),
                        new NuiButton("Upgrade Storage")
                        {
                            Id = "bank_btn_upgrade_storage",
                            Width = 140f,
                            Height = 26f,
                            Visible = CanUpgradeStorage
                        }.Assign(out UpgradeStorageButton)
                    ]
                },
                new NuiLabel(UpgradeStorageCostText)
                {
                    Height = 20f,
                    Visible = CanUpgradeStorage,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer { Height = 4f },
                new NuiRow
                {
                    Height = 160f,
                    Children =
                    [
                        new NuiColumn
                        {
                            Children =
                            [
                                new NuiLabel("Your Inventory (click item to store)")
                                {
                                    Height = 22f,
                                    Width = 330f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle
                                },
                                new NuiList(inventoryRowTemplate, InventoryItemCount)
                                {
                                    RowHeight = 28f,
                                    Height = 132f,
                                    Width = 330f
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
                                    Height = 22f,
                                    Width = 330f,
                                    HorizontalAlign = NuiHAlign.Left,
                                    VerticalAlign = NuiVAlign.Middle
                                },
                                new NuiList(storedItemRowTemplate, PersonalStorageItemCount)
                                {
                                    RowHeight = 28f,
                                    Height = 132f,
                                    Width = 330f
                                }
                            ]
                        }
                    ]
                }
            ]
        };
    }

    private NuiElement BuildFooterButtons()
    {
        return new NuiRow
        {
            // Height = StandardRowHeight + 4f,
            Children =
            [
                new NuiButton("Done")
                {
                    Id = "bank_btn_done",
                    Width = StandardButtonWidth + 10f,
                    Height = StandardButtonHeight
                }.Assign(out DoneButton),
                new NuiSpacer(),
                new NuiButton("Cancel")
                {
                    Id = "bank_btn_cancel",
                    Width = StandardButtonWidth + 10f,
                    Height = StandardButtonHeight
                }.Assign(out CancelButton),
                new NuiSpacer(),
                new NuiButton("Help")
                {
                    Id = "bank_btn_help",
                    Width = StandardButtonWidth + 10f,
                    Height = StandardButtonHeight
                }.Assign(out HelpButton)
            ]
        };
    }
}

public sealed class BankWindowPresenter : ScryPresenter<BankWindowView>, IAutoCloseOnMove
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
    private const string ShareDocumentResRef = "bank_sharedoc";
    private const string ShareDocumentFallbackResRef = "nw_it_mp_scroll001";

    private static class ShareDocumentLocals
    {
        public const string AccountId = "bank_share_account_id";
        public const string CoinhouseTag = "bank_share_coinhouse";
        public const string ShareType = "bank_share_type";
        public const string ShareTypeId = "bank_share_type_id";
        public const string HolderRole = "bank_share_holder_role";
        public const string HolderRoleId = "bank_share_holder_role_id";
        public const string Issuer = "bank_share_issuer";
        public const string DocumentId = "bank_share_document_id";
        public const string IssuedAt = "bank_share_issued_at";
        public const string BankName = "bank_share_bank_name";
    }

    private readonly string _bankDisplayName;
    private readonly CoinhouseTag _coinhouseTag;
    private readonly NwPlayer _player;

    private BankAccountModel? _model;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private List<StoredItem> _foreclosedItems = [];
    private List<StoredItem> _personalStorageItems = [];
    private List<NwItem> _inventoryItems = [];
    private int _personalStorageCapacity = 10;
    private int _personalStorageUsed = 0;

    public BankWindowPresenter(BankWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _coinhouseTag = coinhouseTag;
        _bankDisplayName = string.IsNullOrWhiteSpace(bankDisplayName)
            ? $"Coinhouse ({coinhouseTag.Value})"
            : bankDisplayName;
    }

    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;

    [Inject]
    private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler
    {
        get;
        init;
    } = null!;

    [Inject]
    private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>>
        EligibilityQueryHandler { get; init; } = null!;

    [Inject]
    private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler { get; init; } = null!;

    [Inject] private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler { get; init; } = null!;

    [Inject] private Lazy<ICommandHandler<WithdrawGoldCommand>> WithdrawCommandHandler { get; init; } = null!;

    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; } = null!;

    [Inject] private Lazy<IForeclosureStorageService> ForeclosureStorageService { get; init; } = null!;

    [Inject] private Lazy<IPersonalStorageService> PersonalStorageService { get; init; } = null!;

    [Inject] private WindowDirector WindowDirector { get; init; } = null!;

    private BankAccountModel Model => _model ??= new BankAccountModel(
        AccountQueryHandler.Value,
        EligibilityQueryHandler.Value,
        BankAccessEvaluator.Value);

    public override BankWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _model ??= new BankAccountModel(
            AccountQueryHandler.Value,
            EligibilityQueryHandler.Value,
            BankAccessEvaluator.Value);

        _window = new NuiWindow(View.RootLayout(), _bankDisplayName)
        {
            Geometry = new NuiRect(
                BankWindowView.WindowPosX,
                BankWindowView.WindowPosY,
                BankWindowView.WindowWidth,
                BankWindowView.WindowHeight),
            Resizable = false
        };
    }

    public override async void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey == Guid.Empty)
        {
            Token().Player.SendServerMessage(
                message: "Banking requires a registered character. Please complete character entry before banking.",
                ColorConstants.Orange);
            RaiseCloseEvent();
            Close();
            return;
        }

        CharacterId characterId = CharacterId.From(playerKey);
        PersonaId persona = PersonaId.FromCharacter(characterId);

        try
        {
            Model.SetIdentity(persona, _coinhouseTag, _bankDisplayName);
            await Model.LoadAsync();

            // Load foreclosed items for this character
            await LoadForeclosedItemsAsync(playerKey);

            // Load personal storage items and capacity
            await LoadPersonalStorageAsync(playerKey);

            // Load inventory items for storage
            await LoadInventoryItemsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load bank account for player {PlayerName} at coinhouse {Tag}",
                _player.PlayerName, _coinhouseTag.Value);
            Token().Player.SendServerMessage(
                message: "Bank services are currently unavailable. Please try again later.",
                ColorConstants.Orange);
            RaiseCloseEvent();
            Close();
            return;
        }

        await NwTask.SwitchToMainThread();

        UpdateView();
        ResetAmountInputs();

        if (!Model.AccountExists)
        {
            Token().Player.SendServerMessage(
                message: "You do not yet have an account at this coinhouse. Use the controls above to open one.",
                ColorConstants.Orange);
        }
    }

    public override void UpdateView()
    {
        Token().SetBindValue(View.BankTitle, Model.BankTitle);
        Token().SetBindValue(View.BankSubtitle, Model.Subtitle);

        Token().SetBindValues(View.AccountEntries, Model.AccountEntries);
        Token().SetBindValue(View.AccountEntryCount, Model.AccountEntries.Count);

        Token().SetBindValues(View.HoldingEntries, Model.HoldingEntries);
        Token().SetBindValue(View.HoldingCount, Model.HoldingEntries.Count);

        Token().SetBindValue(View.BalanceText, Model.BalanceLabel);
        Token().SetBindValue(View.LastAccessed, Model.LastAccessedDisplay);

        Token().SetBindValue(View.DepositModeEntries, Model.DepositModeOptions);
        Token().SetBindValue(View.DepositModeSelection, Model.SelectedDepositMode);

        Token().SetBindValue(View.WithdrawModeEntries, Model.WithdrawModeOptions);
        Token().SetBindValue(View.WithdrawModeSelection, Model.SelectedWithdrawMode);

        Token().SetBindValues(View.InventoryItemLabels, Model.DepositInventoryItems);
        Token().SetBindValue(View.InventoryItemCount, Model.DepositInventoryItems.Count);

        Token().SetBindValues(View.PendingDepositLabels, Model.PendingDepositItems);
        Token().SetBindValue(View.PendingDepositCount, Model.PendingDepositItems.Count);

        string eligibilitySummary = Model.AccountExists
            ? "You already have an active account at this coinhouse."
            : Model.EligibilitySummary;

        string personalStatus = Model.AccountExists
            ? "Personal account is already open."
            : Model.PersonalEligibilityStatus;

        string organizationStatus = Model.AccountExists
            ? "Shared account tools will become available after provisioning."
            : Model.OrganizationEligibilityStatus;

        Token().SetBindValue(View.EligibilitySummary, eligibilitySummary);
        Token().SetBindValue(View.PersonalEligibilityStatus, personalStatus);
        Token().SetBindValue(View.OrganizationEligibilityStatus, organizationStatus);

        bool showPersonalActions = !Model.AccountExists;
        Token().SetBindValue(View.ShowPersonalAccountActions, showPersonalActions);

        bool showOrganizationActions = Model.OrganizationEligibility.Any(option => !option.AlreadyHasAccount);
        Token().SetBindValue(View.IsOrganizationLeader, showOrganizationActions);
        Token().SetBindValue(View.ShowShareTools, Model.ShouldShowShareTools);
        Token().SetBindValue(View.ShareTypeEntries, Model.ShareTypeOptions);
        Token().SetBindValue(View.ShareTypeSelection, Model.SelectedShareType);
        Token().SetBindValue(View.ShareInstructions, Model.ShareInstructions);

        // Update foreclosed items display
        bool hasForeclosedItems = _foreclosedItems.Count > 0;
        Token().SetBindValue(View.HasForeclosedItems, hasForeclosedItems);
        Token().SetBindValue(View.ForeclosedItemCount, _foreclosedItems.Count);

        List<string> foreclosedLabels = _foreclosedItems
            .Select((item, index) => $"[{index + 1}] Foreclosed item (ID: {item.Id})")
            .ToList();
        Token().SetBindValues(View.ForeclosedItemLabels, foreclosedLabels);

        // Update personal storage display
        string capacityText = $"Storage Capacity: {_personalStorageUsed}/{_personalStorageCapacity} slots";
        Token().SetBindValue(View.PersonalStorageCapacityText, capacityText);

        Token().SetBindValue(View.PersonalStorageItemCount, _personalStorageItems.Count);
        List<string> storedLabels = _personalStorageItems
            .Select((item, index) => $"[{index + 1}] {item.Name ?? "Unknown Item"}")
            .ToList();
        Token().SetBindValues(View.PersonalStorageItemLabels, storedLabels);

        bool canUpgrade = _personalStorageCapacity < 100;
        Token().SetBindValue(View.CanUpgradeStorage, canUpgrade);

        if (canUpgrade)
        {
            int upgradeCost = PersonalStorageService.Value.CalculateUpgradeCost(_personalStorageCapacity);
            Token().SetBindValue(View.UpgradeStorageCostText,
                $"Next upgrade: +10 slots for {FormatCurrency(upgradeCost)}");
        }
        else
        {
            Token().SetBindValue(View.UpgradeStorageCostText, "Maximum capacity reached");
        }

        if (Model.AccountExists)
        {
            Token().SetBindValue(View.OrganizationAccountEntries, new List<NuiComboEntry>
            {
                new("Account already open", 0)
            });
            Token().SetBindValue(View.OrganizationAccountSelection, 0);
        }
        else
        {
            Token().SetBindValue(View.OrganizationAccountEntries, Model.OrganizationOptions);
            Token().SetBindValue(View.OrganizationAccountSelection, Model.SelectedOrganizationOption);
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click)
        {
            return;
        }

        switch (obj.ElementId)
        {
            case "bank_btn_open_storage":
                OpenStorageWindow();
                break;
            case "bank_btn_open_admin":
                OpenAdminWindow();
                break;
            case "bank_btn_open_personal":
                _ = HandleOpenPersonalAccountAsync();
                break;
            case "bank_btn_open_org":
                _ = HandleOpenOrganizationAccountAsync();
                break;
            case "bank_btn_deposit":
                _ = HandleDepositAsync();
                break;
            case "bank_btn_withdraw":
                _ = HandleWithdrawAsync();
                break;
            case "bank_btn_view_history":
                Token().Player.SendServerMessage("Ledger history will be available in a future update.",
                    ColorConstants.White);
                break;
            case "bank_btn_close_account":
                Token().Player.SendServerMessage("Account closure requires banker assistance.",
                    ColorConstants.White);
                break;
            case "bank_btn_issue_share":
                _ = HandleIssueShareDocumentAsync();
                break;
            case "bank_btn_reclaim_foreclosed":
                _ = HandleReclaimForeclosedItemAsync();
                break;
            case "bank_btn_upgrade_storage":
                _ = HandleUpgradeStorageAsync();
                break;
            case "bank_btn_done":
            case "bank_btn_cancel":
                RaiseCloseEvent();
                Close();
                break;
            case "bank_btn_help":
                Token().Player.SendServerMessage(
                    message: "Visit the banker to learn more about fees, deposits, and letters of credit.",
                    ColorConstants.White);
                break;
        }

        // Handle inventory item clicks (for storing items)
        if (obj.ElementId.StartsWith("bank_inventory_"))
        {
            _ = HandleStoreItemAsync(obj);
        }

        // Handle stored item clicks (for withdrawing items)
        if (obj.ElementId.StartsWith("bank_personal_storage_"))
        {
            _ = HandleWithdrawStoredItemAsync(obj);
        }
    }

    public override void Close()
    {
        _token.Close();
    }

    private async Task HandleOpenPersonalAccountAsync()
    {
        if (Model.AccountExists)
        {
            Token().Player.SendServerMessage("An account already exists for this persona.", ColorConstants.White);
            return;
        }

        if (!Model.CanOpenPersonalAccount)
        {
            string message = string.IsNullOrWhiteSpace(Model.PersonalEligibilityStatus)
                ? "You cannot open a personal account right now."
                : Model.PersonalEligibilityStatus;
            Token().Player.SendServerMessage(message, ColorConstants.Orange);
            return;
        }

        int deposit = Model.PersonalOpeningDeposit;
        if (!await HasSufficientFundsAsync(deposit, "open this account"))
        {
            return;
        }

        string? displayName = _player.LoginCreature?.Name?.Trim();

        OpenCoinhouseAccountCommand command = new(
            Model.Persona,
            Model.Persona,
            _coinhouseTag,
            string.IsNullOrWhiteSpace(displayName) ? null : displayName);

        if (deposit <= 0)
        {
            await ProcessOpenPersonalAccountAsync(command, deposit);
            return;
        }

        string messageBody =
            $"Opening a personal coinhouse account requires {FormatCurrency(deposit)}. Do you wish to proceed?";

        WindowDirector.OpenPopupWithReaction(
            _player,
            "Confirm Personal Account",
            messageBody,
            () => _ = ProcessOpenPersonalAccountAsync(command, deposit),
            linkedToken: Token());
    }

    private async Task ProcessOpenPersonalAccountAsync(OpenCoinhouseAccountCommand command, int deposit)
    {
        bool goldDeducted = false;

        try
        {
            if (deposit > 0)
            {
                goldDeducted = await TryWithdrawGoldAsync(deposit, "open this account");
                if (!goldDeducted)
                {
                    return;
                }
            }

            CommandResult openResult;
            try
            {
                openResult = await OpenAccountCommandHandler.Value.HandleAsync(command);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open personal account for player {PlayerName} at coinhouse {Tag}",
                    _player.PlayerName, _coinhouseTag.Value);

                if (goldDeducted)
                {
                    await RefundGoldAsync(deposit);
                }

                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage(
                    message: "The banker could not create your account due to an unexpected error.",
                    ColorConstants.Orange);
                return;
            }

            if (!openResult.Success)
            {
                if (goldDeducted)
                {
                    await RefundGoldAsync(deposit);
                }

                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage(
                    message: openResult.ErrorMessage ?? "Unable to open a personal account.",
                    ColorConstants.Orange);
                return;
            }

            if (deposit > 0)
            {
                CommandResult depositResult;
                try
                {
                    DepositGoldCommand depositCommand = DepositGoldCommand.Create(
                        Model.Persona,
                        _coinhouseTag,
                        deposit,
                        "Opening deposit");

                    depositResult = await DepositCommandHandler.Value.HandleAsync(depositCommand);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to record opening deposit for player {PlayerName} at coinhouse {Tag}",
                        _player.PlayerName, _coinhouseTag.Value);
                    depositResult = CommandResult.Fail("Failed to record the opening deposit.");
                }

                if (!depositResult.Success)
                {
                    if (goldDeducted)
                    {
                        await RefundGoldAsync(deposit);
                    }

                    await NwTask.SwitchToMainThread();
                    Token().Player.SendServerMessage(
                        message:
                        "Your account was created but the opening deposit failed. Your gold has been returned.",
                        ColorConstants.Orange);
                    return;
                }
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Your personal coinhouse account has been opened.",
                ColorConstants.White);

            await ReloadModelAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Unexpected failure while opening personal account for player {PlayerName} at coinhouse {Tag}",
                _player.PlayerName, _coinhouseTag.Value);

            if (goldDeducted)
            {
                await RefundGoldAsync(deposit);
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "The banker could not create your account due to an unexpected error.",
                ColorConstants.Orange);
        }
    }

    private async Task<bool> HasSufficientFundsAsync(int required, string purpose)
    {
        if (required <= 0)
        {
            return true;
        }

        await NwTask.SwitchToMainThread();

        NwCreature? creature = _player.LoginCreature;
        if (creature is null)
        {
            Token().Player.SendServerMessage(
                message: $"You must be possessing a character to {purpose}.",
                ColorConstants.Orange);
            return false;
        }

        uint available = creature.Gold;
        uint requiredGold = (uint)Math.Max(required, 0);

        if (available < requiredGold)
        {
            Token().Player.SendServerMessage(
                message: $"You need {FormatCurrency(required)} on hand to {purpose}.",
                ColorConstants.Orange);
            return false;
        }

        return true;
    }

    private async Task<bool> TryWithdrawGoldAsync(int amount, string purpose)
    {
        if (amount <= 0)
        {
            return true;
        }

        await NwTask.SwitchToMainThread();

        NwCreature? creature = _player.LoginCreature;
        if (creature is null)
        {
            Token().Player.SendServerMessage(
                message: $"You must be possessing a character to {purpose}.",
                ColorConstants.Orange);
            return false;
        }

        uint available = creature.Gold;
        uint requested = (uint)Math.Max(amount, 0);

        if (available < requested)
        {
            Token().Player.SendServerMessage(
                message: $"You need {FormatCurrency(amount)} on hand to {purpose}.",
                ColorConstants.Orange);
            return false;
        }

        creature.Gold = available - requested;
        return true;
    }

    private async Task RefundGoldAsync(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        NwCreature? creature = _player.LoginCreature;
        if (creature is not null)
        {
            creature.Gold = creature.Gold + (uint)Math.Max(amount, 0);
        }
    }

    private static string FormatCurrency(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture) + " gp";
    }

    private async Task HandleOpenOrganizationAccountAsync()
    {
        if (Model.AccountExists)
        {
            Token().Player.SendServerMessage("An account is already active for this coinhouse.", ColorConstants.White);
            return;
        }

        int selected = Token().GetBindValue(View.OrganizationAccountSelection);
        Model.SelectedOrganizationOption = selected;

        OrganizationAccountEligibility? option = Model.GetOrganizationSelection(selected);
        if (option is null)
        {
            Token().Player.SendServerMessage(
                message: "Select an organization before opening an account.",
                ColorConstants.Orange);
            return;
        }

        if (!option.CanOpen)
        {
            string reason = option.BlockedReason ?? "That organization cannot open an account right now.";
            Token().Player.SendServerMessage(reason, ColorConstants.Orange);
            return;
        }

        PersonaId organizationPersona = PersonaId.FromOrganization(option.OrganizationId);

        List<CoinhouseAccountHolderDto> additional = new();

        if (Guid.TryParse(Model.Persona.Value, out Guid requestorGuid))
        {
            string? requestorLabel = _player.LoginCreature?.Name?.Trim();
            string fallbackName = string.IsNullOrWhiteSpace(requestorLabel)
                ? (_player.PlayerName ?? "Unknown Player")
                : requestorLabel;
            additional.Add(new CoinhouseAccountHolderDto
            {
                HolderId = requestorGuid,
                Type = HolderType.Individual,
                Role = HolderRole.Signatory,
                FirstName = fallbackName,
                LastName = string.Empty
            });
        }

        OpenCoinhouseAccountCommand command = new(
            Model.Persona,
            organizationPersona,
            _coinhouseTag,
            option.OrganizationName,
            additional);

        CommandResult result;
        try
        {
            result = await OpenAccountCommandHandler.Value.HandleAsync(command);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open organization account {Organization} at coinhouse {Tag}",
                option.OrganizationName, _coinhouseTag.Value);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "The banker could not create the organization account due to an unexpected error.",
                ColorConstants.Orange);
            return;
        }

        await NwTask.SwitchToMainThread();

        if (!result.Success)
        {
            Token().Player.SendServerMessage(
                message: result.ErrorMessage ?? "Unable to open the organization account.",
                ColorConstants.Orange);
            return;
        }

        Token().Player.SendServerMessage(
            message: $"{option.OrganizationName} now maintains an account at this coinhouse.",
            ColorConstants.White);

        await ReloadModelAsync();
    }

    private async Task HandleIssueShareDocumentAsync()
    {
        if (!Model.AccountExists)
        {
            Token().Player.SendServerMessage(
                message: "Open an account before issuing share documents.",
                ColorConstants.White);
            return;
        }

        if (!Model.CanIssueShares)
        {
            Token().Player.SendServerMessage(
                message: "You are not authorized to issue share documents for this account.",
                ColorConstants.White);
            return;
        }

        int selected = Token().GetBindValue(View.ShareTypeSelection);
        Model.SelectedShareType = selected;

        if (!Model.TryGetShareType(selected, out BankShareType shareType))
        {
            Model.SelectedShareType = (int)BankShareType.JointOwner;
            Token().SetBindValue(View.ShareTypeSelection, Model.SelectedShareType);
            Token().Player.SendServerMessage(
                message: "Select a share role before issuing a document.",
                ColorConstants.Orange);
            return;
        }

        if (_player.LoginCreature is null)
        {
            Token().Player.SendServerMessage(
                message: "You must be possessing a character to issue share documents.",
                ColorConstants.Orange);
            return;
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(Model.Persona, Model.Coinhouse);
        Guid documentId = Guid.NewGuid();
        HolderRole holderRole = shareType.ToHolderRole();
        DateTime issuedAt = DateTime.UtcNow;

        try
        {
            await NwTask.SwitchToMainThread();

            NwCreature? creature = _player.LoginCreature;
            if (creature?.Location == null)
            {
                Token().Player.SendServerMessage(
                    message: "Share documents cannot be issued right now. Please try again shortly.",
                    ColorConstants.Orange);
                return;
            }

            NwItem? document = NwItem.Create(ShareDocumentResRef, creature.Location);
            if (document is null)
            {
                Log.Warn("Share document blueprint '{ResRef}' was not found. Falling back to '{Fallback}'.",
                    ShareDocumentResRef, ShareDocumentFallbackResRef);
                document = NwItem.Create(ShareDocumentFallbackResRef, creature.Location);
            }

            if (document is null)
            {
                Token().Player.SendServerMessage(
                    message: "The bank cannot produce share documents at this time.",
                    ColorConstants.Orange);
                return;
            }

            string issuerName = creature.Name ?? _player.PlayerName ?? "Unknown Issuer";
            string documentName = FormatShareDocumentName(shareType);

            document.Tag = documentId.ToString("N");
            document.Name = documentName;
            document.Description = BuildShareDocumentDescription(
                shareType,
                accountId,
                documentId,
                issuerName,
                issuedAt);
            document.Identified = true;
            document.StackSize = 1;

            NWScript.SetLocalString(document, ShareDocumentLocals.AccountId, accountId.ToString());
            NWScript.SetLocalString(document, ShareDocumentLocals.CoinhouseTag, Model.Coinhouse.Value ?? string.Empty);
            NWScript.SetLocalString(document, ShareDocumentLocals.ShareType, shareType.ToString());
            NWScript.SetLocalInt(document, ShareDocumentLocals.ShareTypeId, (int)shareType);
            NWScript.SetLocalString(document, ShareDocumentLocals.HolderRole, holderRole.ToString());
            NWScript.SetLocalInt(document, ShareDocumentLocals.HolderRoleId, (int)holderRole);
            NWScript.SetLocalString(document, ShareDocumentLocals.Issuer, issuerName);
            NWScript.SetLocalString(document, ShareDocumentLocals.DocumentId, documentId.ToString());
            NWScript.SetLocalString(document, ShareDocumentLocals.IssuedAt,
                issuedAt.ToString("o", CultureInfo.InvariantCulture));
            NWScript.SetLocalString(document, ShareDocumentLocals.BankName, Model.BankTitle);

            creature.AcquireItem(document);

            Token().Player.SendServerMessage(
                message: $"A {documentName} has been added to your inventory.",
                ColorConstants.White);

            Log.Info(
                "Issued bank share document {DocumentId} for account {AccountId} ({ShareType}) at coinhouse {Coinhouse}.",
                documentId, accountId, shareType, Model.Coinhouse.Value ?? "(unknown)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create share document for account {AccountId} at coinhouse {Coinhouse}.",
                accountId, Model.Coinhouse.Value ?? "(unknown)");

            Token().Player.SendServerMessage(
                message: "The banker failed to prepare the share document. Please try again later.",
                ColorConstants.Orange);
        }
        finally
        {
            Token().SetBindValue(View.ShareTypeSelection, Model.SelectedShareType);
            Token().SetBindValue(View.ShareInstructions, Model.ShareInstructions);
        }
    }

    private string FormatShareDocumentName(BankShareType shareType)
    {
        string roleName = Model.ShareRoleName(shareType);
        return $"{Model.BankTitle} Share ({roleName})";
    }

    private string BuildShareDocumentDescription(
        BankShareType shareType,
        Guid accountId,
        Guid documentId,
        string issuerName,
        DateTime issuedAt)
    {
        string roleName = Model.ShareRoleName(shareType);
        string summary = Model.ShareRoleSummary(shareType);
        string coinhouseTag = Model.Coinhouse.Value ?? "Unspecified";

        return
            $"{Model.BankTitle}\nShare Role: {roleName}\nScope: {summary}\nCoinhouse: {coinhouseTag}\nAccount Reference: {accountId}\nDocument: {documentId}\nIssuer: {issuerName}\nIssued (UTC): {issuedAt.ToString("g", CultureInfo.InvariantCulture)}\n\nPresent this parchment at the banker to register the share.";
    }

    private async Task HandleDepositAsync()
    {
        if (!Model.AccountExists)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "You need an open account before the banker can accept coin.",
                ColorConstants.Orange);
            return;
        }

        if (!Model.CanDeposit)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "You are not authorized to deposit coin into this account.",
                ColorConstants.Orange);
            return;
        }

        int selectedMode = Token().GetBindValue(View.DepositModeSelection);
        Model.SelectedDepositMode = selectedMode;

        if (selectedMode != 0)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "For now the banker only accepts coins handed over at the counter.",
                ColorConstants.White);
            return;
        }

        string rawAmount = Token().GetBindValue(View.DepositAmountText) ?? string.Empty;
        if (!TryParseAmount(rawAmount, out int amount, out string sanitized))
        {
            Token().SetBindValue(View.DepositAmountText, sanitized);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Tell the banker how many coins you wish to deposit.",
                ColorConstants.Orange);
            return;
        }

        Token().SetBindValue(View.DepositAmountText, sanitized);

        bool goldDeducted = false;
        try
        {
            goldDeducted = await TryWithdrawGoldAsync(amount, "deposit into your coinhouse account");
            if (!goldDeducted)
            {
                return;
            }

            CommandResult result;
            try
            {
                DepositGoldCommand command = DepositGoldCommand.Create(
                    Model.Persona,
                    _coinhouseTag,
                    amount,
                    "Counter deposit");

                result = await DepositCommandHandler.Value.HandleAsync(command);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to record deposit for player {PlayerName} at coinhouse {Tag}",
                    _player.PlayerName, _coinhouseTag.Value);
                result = CommandResult.Fail("The ledger could not be updated with your deposit.");
            }

            if (!result.Success)
            {
                if (goldDeducted)
                {
                    await RefundGoldAsync(amount);
                }

                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage(
                    message: result.ErrorMessage ?? "The banker could not accept your deposit.",
                    ColorConstants.Orange);
                return;
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: $"Deposited {FormatCurrency(amount)} into your account.",
                ColorConstants.White);

            ResetAmountInputs();
            await ReloadModelAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected failure while depositing for player {PlayerName} at coinhouse {Tag}",
                _player.PlayerName, _coinhouseTag.Value);

            if (goldDeducted)
            {
                await RefundGoldAsync(amount);
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "The banker fumbles the ledger. Your gold has been returned.",
                ColorConstants.Orange);
        }
    }

    private async Task HandleWithdrawAsync()
    {
        if (!Model.AccountExists)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "There is no account to draw coin from yet.",
                ColorConstants.Orange);
            return;
        }

        if (!Model.CanWithdraw)
        {
            await NwTask.SwitchToMainThread();
            string message = Model.CanRequestWithdraw
                ? "You may only request a withdrawal. Present your request to a banker."
                : "You are not authorized to withdraw from this account.";

            Token().Player.SendServerMessage(message, ColorConstants.Orange);
            return;
        }

        int selectedMode = Token().GetBindValue(View.WithdrawModeSelection);
        Model.SelectedWithdrawMode = selectedMode;

        if (selectedMode != 0)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Withdrawals are presently paid out in coins at the counter.",
                ColorConstants.White);
            return;
        }

        string rawAmount = Token().GetBindValue(View.WithdrawAmountText) ?? string.Empty;
        if (!TryParseAmount(rawAmount, out int amount, out string sanitized))
        {
            Token().SetBindValue(View.WithdrawAmountText, sanitized);
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Let the banker know how many coins you require.",
                ColorConstants.Orange);
            return;
        }

        Token().SetBindValue(View.WithdrawAmountText, sanitized);

        if (Model.CurrentBalance < amount)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: $"You only have {FormatCurrency(Model.CurrentBalance)} available to withdraw.",
                ColorConstants.Orange);
            return;
        }

        CommandResult result;
        try
        {
            WithdrawGoldCommand command = WithdrawGoldCommand.Create(
                Model.Persona,
                _coinhouseTag,
                amount,
                "Counter withdrawal");

            result = await WithdrawCommandHandler.Value.HandleAsync(command);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to record withdrawal for player {PlayerName} at coinhouse {Tag}",
                _player.PlayerName, _coinhouseTag.Value);
            result = CommandResult.Fail("The ledger could not be updated with your withdrawal.");
        }

        if (!result.Success)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: result.ErrorMessage ?? "The banker could not fulfill your withdrawal.",
                ColorConstants.Orange);
            return;
        }

        await RefundGoldAsync(amount); // Reuse helper to credit coins to the player.

        await NwTask.SwitchToMainThread();
        Token().Player.SendServerMessage(
            message: $"Withdrew {FormatCurrency(amount)} from your account.",
            ColorConstants.White);

        ResetAmountInputs();
        await ReloadModelAsync();
    }

    private async Task ReloadModelAsync()
    {
        try
        {
            await Model.LoadAsync();
            await NwTask.SwitchToMainThread();
            UpdateView();
            ResetAmountInputs();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh bank window after provisioning for player {PlayerName} at {Tag}",
                _player.PlayerName, _coinhouseTag.Value);
            Token().Player.SendServerMessage(
                message: "The bank window failed to refresh. Please close and reopen it.",
                ColorConstants.Orange);
        }
    }

    private void ResetAmountInputs()
    {
        Token().SetBindValue(View.DepositAmountText, string.Empty);
        Token().SetBindValue(View.WithdrawAmountText, string.Empty);
    }

    private static bool TryParseAmount(string? raw, out int amount, out string sanitized)
    {
        string source = raw?.Trim() ?? string.Empty;
        sanitized = new string(source.Where(char.IsDigit).ToArray());

        if (sanitized.Length == 0)
        {
            amount = 0;
            return false;
        }

        if (!int.TryParse(sanitized, NumberStyles.None, CultureInfo.InvariantCulture, out amount))
        {
            amount = 0;
            return false;
        }

        if (amount <= 0)
        {
            amount = 0;
            return false;
        }

        return true;
    }

    private async Task LoadForeclosedItemsAsync(Guid characterId)
    {
        try
        {
            _foreclosedItems = await ForeclosureStorageService.Value.GetForeclosedItemsAsync(
                _coinhouseTag,
                characterId);

            Log.Info("Loaded {Count} foreclosed items for character {CharacterId} at coinhouse {Coinhouse}",
                _foreclosedItems.Count, characterId, _coinhouseTag.Value);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load foreclosed items for character {CharacterId} at coinhouse {Coinhouse}",
                characterId, _coinhouseTag.Value);
            _foreclosedItems = [];
        }
    }

    private async Task HandleReclaimForeclosedItemAsync()
    {
        if (_foreclosedItems.Count == 0)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "You have no foreclosed items to reclaim at this coinhouse.",
                ColorConstants.White);
            return;
        }

        // For simplicity, we'll reclaim all items at once
        // In a more sophisticated UI, you'd let players select which items to reclaim

        await NwTask.SwitchToMainThread();

        NwCreature? creature = _player.LoginCreature;
        if (creature is null)
        {
            Token().Player.SendServerMessage(
                message: "You must be possessing a character to reclaim items.",
                ColorConstants.Orange);
            return;
        }

        int reclaimedCount = 0;
        int failedCount = 0;
        List<long> itemsToRemove = [];

        foreach (StoredItem storedItem in _foreclosedItems)
        {
            try
            {
                // Deserialize the item
                NwItem? item = NwItem.Deserialize(storedItem.ItemData);

                if (item is null)
                {
                    Log.Warn("Failed to deserialize foreclosed item {ItemId} for player {PlayerName}",
                        storedItem.Id, _player.PlayerName);
                    failedCount++;
                    continue;
                }

                // Give item to player
                creature.AcquireItem(item);

                itemsToRemove.Add(storedItem.Id);
                reclaimedCount++;

                Log.Info("Player {PlayerName} reclaimed foreclosed item {ItemId} from coinhouse {Coinhouse}",
                    _player.PlayerName, storedItem.Id, _coinhouseTag.Value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reclaiming foreclosed item {ItemId} for player {PlayerName}",
                    storedItem.Id, _player.PlayerName);
                failedCount++;
            }
        }

        // Remove successfully reclaimed items from storage
        foreach (long itemId in itemsToRemove)
        {
            try
            {
                await ForeclosureStorageService.Value.RemoveForeclosedItemAsync(itemId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to remove foreclosed item {ItemId} from storage", itemId);
            }
        }

        await NwTask.SwitchToMainThread();

        if (reclaimedCount > 0)
        {
            string message = reclaimedCount == 1
                ? "Reclaimed 1 foreclosed item."
                : $"Reclaimed {reclaimedCount} foreclosed items.";

            if (failedCount > 0)
            {
                message += $" ({failedCount} items could not be recovered.)";
            }

            Token().Player.SendServerMessage(message, ColorConstants.White);
        }
        else
        {
            Token().Player.SendServerMessage(
                message: "No items could be reclaimed. Please contact a DM if this persists.",
                ColorConstants.Orange);
        }

        // Reload foreclosed items and update view
        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey != Guid.Empty)
        {
            await LoadForeclosedItemsAsync(playerKey);
            await NwTask.SwitchToMainThread();
            UpdateView();
        }
    }

    private async Task LoadPersonalStorageAsync(Guid characterId)
    {
        try
        {
            StorageCapacityInfo capacityInfo = await PersonalStorageService.Value.GetStorageCapacityAsync(
                _coinhouseTag,
                characterId);

            _personalStorageCapacity = capacityInfo.Capacity;
            _personalStorageUsed = capacityInfo.UsedSlots;

            _personalStorageItems = await PersonalStorageService.Value.GetStoredItemsAsync(
                _coinhouseTag,
                characterId);

            Log.Info(
                "Loaded {Count} personal storage items for character {CharacterId} at coinhouse {Coinhouse}. Capacity: {Used}/{Total}",
                _personalStorageItems.Count, characterId, _coinhouseTag.Value, _personalStorageUsed,
                _personalStorageCapacity);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load personal storage for character {CharacterId} at coinhouse {Coinhouse}",
                characterId, _coinhouseTag.Value);
            _personalStorageItems = [];
            _personalStorageCapacity = 10;
            _personalStorageUsed = 0;
        }
    }

    private async Task LoadInventoryItemsAsync()
    {
        await NwTask.SwitchToMainThread();

        _inventoryItems.Clear();

        NwCreature? creature = _player.LoginCreature;
        if (creature is null)
        {
            return;
        }

        foreach (NwItem item in creature.Inventory.Items)
        {
            // Skip equipped items
            bool isEquipped = false;
            foreach (InventorySlot slot in Enum.GetValues<InventorySlot>())
            {
                if (creature.GetItemInSlot(slot) == item)
                {
                    isEquipped = true;
                    break;
                }
            }

            if (isEquipped)
            {
                continue;
            }

            _inventoryItems.Add(item);
        }
    }

    private async Task HandleUpgradeStorageAsync()
    {
        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey == Guid.Empty)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Storage upgrades require a registered character.",
                ColorConstants.Orange);
            return;
        }

        if (_personalStorageCapacity >= 100)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Your storage is already at maximum capacity.",
                ColorConstants.White);
            return;
        }

        int upgradeCost = PersonalStorageService.Value.CalculateUpgradeCost(_personalStorageCapacity);

        if (!await HasSufficientFundsAsync(upgradeCost, "upgrade your storage"))
        {
            return;
        }

        string messageBody =
            $"Upgrading your storage from {_personalStorageCapacity} to {_personalStorageCapacity + 10} slots costs {FormatCurrency(upgradeCost)}. Proceed?";

        WindowDirector.OpenPopupWithReaction(
            _player,
            "Confirm Storage Upgrade",
            messageBody,
            () => _ = ProcessUpgradeStorageAsync(playerKey, upgradeCost),
            linkedToken: Token());
    }

    private async Task ProcessUpgradeStorageAsync(Guid characterId, int cost)
    {
        bool goldDeducted = false;

        try
        {
            goldDeducted = await TryWithdrawGoldAsync(cost, "upgrade your storage");
            if (!goldDeducted)
            {
                return;
            }

            StorageUpgradeResult result = await PersonalStorageService.Value.UpgradeStorageCapacityAsync(
                _coinhouseTag,
                characterId);

            if (!result.Success)
            {
                if (goldDeducted)
                {
                    await RefundGoldAsync(cost);
                }

                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage(
                    message: result.Message ?? "Storage upgrade failed.",
                    ColorConstants.Orange);
                return;
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: $"Storage upgraded to {result.NewCapacity} slots!",
                ColorConstants.White);

            // Reload personal storage
            await LoadPersonalStorageAsync(characterId);
            UpdateView();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upgrade storage for character {CharacterId} at coinhouse {Coinhouse}",
                characterId, _coinhouseTag.Value);

            if (goldDeducted)
            {
                await RefundGoldAsync(cost);
            }

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Storage upgrade failed due to an unexpected error.",
                ColorConstants.Orange);
        }
    }

    private async Task HandleStoreItemAsync(ModuleEvents.OnNuiEvent obj)
    {
        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey == Guid.Empty)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Storing items requires a registered character.",
                ColorConstants.Orange);
            return;
        }

        // Extract index from element ID (e.g., "bank_inventory_3" -> 3)
        if (!int.TryParse(obj.ElementId.Replace("bank_inventory_", ""), out int index))
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        if (index < 0 || index >= _inventoryItems.Count)
        {
            Token().Player.SendServerMessage(
                message: "Selected item is no longer available.",
                ColorConstants.Orange);
            return;
        }

        NwItem item = _inventoryItems[index];
        if (item == null || !item.IsValid)
        {
            Token().Player.SendServerMessage(
                message: "Selected item is no longer valid.",
                ColorConstants.Orange);
            await LoadInventoryItemsAsync();
            UpdateView();
            return;
        }

        string itemName = item.Name ?? "Unknown Item";

        try
        {
            byte[] itemData = item.Serialize();

            StorageResult result = await PersonalStorageService.Value.StoreItemAsync(
                _coinhouseTag,
                playerKey,
                itemName,
                itemData);

            await NwTask.SwitchToMainThread();

            if (!result.Success)
            {
                Token().Player.SendServerMessage(
                    message: result.Message ?? "Failed to store item.",
                    ColorConstants.Orange);
                return;
            }

            // Destroy the item from inventory
            item.Destroy();

            Token().Player.SendServerMessage(
                message: $"Stored {itemName} in your personal storage.",
                ColorConstants.White);

            // Reload inventory and storage
            await LoadInventoryItemsAsync();
            await LoadPersonalStorageAsync(playerKey);
            UpdateView();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to store item {ItemName} for character {CharacterId}",
                itemName, playerKey);

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Failed to store item due to an unexpected error.",
                ColorConstants.Orange);
        }
    }

    private async Task HandleWithdrawStoredItemAsync(ModuleEvents.OnNuiEvent obj)
    {
        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey == Guid.Empty)
        {
            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Withdrawing items requires a registered character.",
                ColorConstants.Orange);
            return;
        }

        // Extract index from element ID (e.g., "bank_personal_storage_2" -> 2)
        if (!int.TryParse(obj.ElementId.Replace("bank_personal_storage_", ""), out int index))
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        if (index < 0 || index >= _personalStorageItems.Count)
        {
            Token().Player.SendServerMessage(
                message: "Selected item is no longer available.",
                ColorConstants.Orange);
            return;
        }

        StoredItem storedItem = _personalStorageItems[index];
        string itemName = storedItem.Name ?? "Unknown Item";

        try
        {
            StoredItem? withdrawnItem = await PersonalStorageService.Value.WithdrawItemAsync(
                storedItem.Id,
                playerKey);

            if (withdrawnItem == null)
            {
                await NwTask.SwitchToMainThread();
                Token().Player.SendServerMessage(
                    message: "Failed to withdraw item from storage.",
                    ColorConstants.Orange);
                return;
            }

            await NwTask.SwitchToMainThread();

            NwCreature? creature = _player.LoginCreature;
            if (creature?.Location == null)
            {
                Token().Player.SendServerMessage(
                    message: "Cannot withdraw items at this time.",
                    ColorConstants.Orange);

                // Try to return item to storage
                try
                {
                    StorageResult restoreResult = await PersonalStorageService.Value.StoreItemAsync(
                        _coinhouseTag,
                        playerKey,
                        withdrawnItem.Name ?? "Unknown Item",
                        withdrawnItem.ItemData);

                    if (!restoreResult.Success)
                    {
                        Log.Error("Failed to restore item {ItemId} to storage after failed withdrawal", storedItem.Id);
                    }
                }
                catch (Exception restoreEx)
                {
                    Log.Error(restoreEx, "Exception while restoring item {ItemId} after failed withdrawal",
                        storedItem.Id);
                }

                return;
            }

            // Deserialize and give item to player
            NwItem? item = NwItem.Deserialize(withdrawnItem.ItemData);
            if (item == null)
            {
                Token().Player.SendServerMessage(
                    message: "Failed to restore the stored item. Please contact a DM.",
                    ColorConstants.Orange);
                Log.Error("Failed to deserialize stored item {ItemId} ({ItemName}) for character {CharacterId}",
                    storedItem.Id, itemName, playerKey);
                return;
            }

            creature.AcquireItem(item);

            Token().Player.SendServerMessage(
                message: $"Withdrew {itemName} from your personal storage.",
                ColorConstants.White);

            // Reload inventory and storage
            await LoadInventoryItemsAsync();
            await LoadPersonalStorageAsync(playerKey);
            UpdateView();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to withdraw item {ItemId} ({ItemName}) for character {CharacterId}",
                storedItem.Id, itemName, playerKey);

            await NwTask.SwitchToMainThread();
            Token().Player.SendServerMessage(
                message: "Failed to withdraw item due to an unexpected error.",
                ColorConstants.Orange);
        }
    }

    private void OpenStorageWindow()
    {
        BankStorageWindowView storageWindow = new(_player, _coinhouseTag, _bankDisplayName);
        storageWindow.Presenter.Create();
    }

    private void OpenAdminWindow()
    {
        BankAdminWindowView adminWindow = new(_player, _coinhouseTag, _bankDisplayName);
        adminWindow.Presenter.Create();
    }
}
