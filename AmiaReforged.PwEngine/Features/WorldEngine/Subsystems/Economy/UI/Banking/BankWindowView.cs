using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking;

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
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 900f, 700f))]
                },
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
                            HorizontalAlign = NuiHAlign.Left,
                            ForegroundColor = new Color(50, 40, 30)
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
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(50, 40, 30)
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
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
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
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiLabel(BankSubtitle)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
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
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiLabel(LastAccessed)
                {
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
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
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
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
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
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
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
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
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
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
