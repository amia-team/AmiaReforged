using System;
using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Front end for player bank interactions.
/// </summary>
public sealed class BankWindowView : ScryView<BankWindowPresenter>
{
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
    public readonly NuiBind<List<NuiComboEntry>> WithdrawModeEntries = new("bank_withdraw_mode_entries");
    public readonly NuiBind<int> WithdrawModeSelection = new("bank_withdraw_mode_selection");
    public readonly NuiBind<int> InventoryItemCount = new("bank_inventory_item_count");
    public readonly NuiBind<string> InventoryItemLabels = new("bank_inventory_item_labels");
    public readonly NuiBind<int> PendingDepositCount = new("bank_pending_deposit_count");
    public readonly NuiBind<string> PendingDepositLabels = new("bank_pending_deposit_labels");

    public NuiButton DepositButton = null!;
    public NuiButton WithdrawButton = null!;
    public NuiButton TransferButton = null!;
    public NuiButton ConfirmDepositButton = null!;
    public NuiButton ViewHistoryButton = null!;
    public NuiButton CloseAccountButton = null!;
    public NuiButton DoneButton = null!;
    public NuiButton CancelButton = null!;
    public NuiButton HelpButton = null!;

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

        NuiColumn root = new()
        {
            Children =
            [
                BuildHeader(),
                new NuiSpacer { Height = 6f },
                BuildControlsRow(),
                new NuiSpacer { Height = 4f },
                BuildMainContent(accountRowTemplate, holdingRowTemplate, inventoryRowTemplate, pendingRowTemplate),
                new NuiSpacer { Height = 6f },
                BuildSecondaryActions(),
                new NuiSpacer { Height = 6f },
                BuildFooterButtons()
            ]
        };

        return root;
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

    private NuiElement BuildControlsRow()
    {
        return new NuiRow
        {
            Height = 38f,
            Children =
            [
                new NuiLabel("Your Accounts")
                {
                    Width = 220f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiSpacer(),
                new NuiButton("Deposit")
                {
                    Id = "bank_btn_deposit",
                    Width = 100f,
                    Height = 32f
                }.Assign(out DepositButton),
                new NuiButton("Withdraw")
                {
                    Id = "bank_btn_withdraw",
                    Width = 100f,
                    Height = 32f
                }.Assign(out WithdrawButton),
                new NuiButton("Transfer")
                {
                    Id = "bank_btn_transfer",
                    Width = 100f,
                    Height = 32f
                }.Assign(out TransferButton)
            ]
        };
    }

    private NuiElement BuildMainContent(
        IReadOnlyList<NuiListTemplateCell> accountRowTemplate,
        IReadOnlyList<NuiListTemplateCell> holdingRowTemplate,
        IReadOnlyList<NuiListTemplateCell> inventoryRowTemplate,
        IReadOnlyList<NuiListTemplateCell> pendingRowTemplate)
    {
        NuiColumn leftColumn = new()
        {
            Width = 280f,
            Children =
            [
                new NuiList(accountRowTemplate, AccountEntryCount)
                {
                    RowHeight = 32f,
                    Height = 110f,
                    Width = 260f
                },
                new NuiSpacer { Height = 6f },
                new NuiList(holdingRowTemplate, HoldingCount)
                {
                    RowHeight = 30f,
                    Height = 100f,
                    Width = 260f
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
                    Height = 36f,
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
                new NuiSpacer { Height = 8f },
                new NuiLabel("Pending Transactions")
                {
                    Height = 22f
                },
                new NuiList(pendingRowTemplate, PendingDepositCount)
                {
                    RowHeight = 28f,
                    Height = 90f,
                    Width = 260f
                }
            ]
        };

        NuiColumn rightColumn = new()
        {
            Width = 330f,
            Children =
            [
                new NuiLabel("Items to Deposit:")
                {
                    Height = 22f
                },
                new NuiList(inventoryRowTemplate, InventoryItemCount)
                {
                    RowHeight = 30f,
                    Height = 220f,
                    Width = 310f
                },
                new NuiSpacer { Height = 8f },
                new NuiButton("Confirm Deposit")
                {
                    Id = "bank_btn_confirm_deposit",
                    Height = 34f
                }.Assign(out ConfirmDepositButton)
            ]
        };

        return new NuiRow
        {
            Children =
            [
                leftColumn,
                new NuiSpacer { Width = 18f },
                rightColumn
            ]
        };
    }

    private NuiElement BuildSecondaryActions()
    {
        return new NuiRow
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
        };
    }

    private NuiElement BuildFooterButtons()
    {
        return new NuiRow
        {
            Height = 40f,
            Children =
            [
                new NuiButton("Done")
                {
                    Id = "bank_btn_done",
                    Width = 110f,
                    Height = 32f
                }.Assign(out DoneButton),
                new NuiSpacer(),
                new NuiButton("Cancel")
                {
                    Id = "bank_btn_cancel",
                    Width = 110f,
                    Height = 32f
                }.Assign(out CancelButton),
                new NuiSpacer(),
                new NuiButton("Help")
                {
                    Id = "bank_btn_help",
                    Width = 110f,
                    Height = 32f
                }.Assign(out HelpButton)
            ]
        };
    }
}


public sealed class BankWindowPresenter : ScryPresenter<BankWindowView>
{
    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

    private readonly string _bankDisplayName;
    private readonly CoinhouseTag _coinhouseTag;
    private readonly NwPlayer _player;

    private BankAccountModel? _model;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public BankWindowPresenter(BankWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _coinhouseTag = coinhouseTag;
        _bankDisplayName = string.IsNullOrWhiteSpace(bankDisplayName)
            ? $"Coinhouse ({coinhouseTag.Value})"
            : bankDisplayName;
    }

    [Inject] private Lazy<AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;
    [Inject]
    private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler { get; init; }
        = null!;

    private BankAccountModel Model => _model ??= new BankAccountModel(AccountQueryHandler.Value);

    public override BankWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _model ??= new BankAccountModel(AccountQueryHandler.Value);

        _window = new NuiWindow(View.RootLayout(), _bankDisplayName)
        {
            Geometry = new NuiRect(120f, 120f, 720f, 540f),
            Resizable = true
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

        if (!Model.AccountExists)
        {
            Token().Player.SendServerMessage(
                message: "You do not yet have an account at this coinhouse. Speak with the banker to open one.",
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
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click)
        {
            return;
        }

        switch (obj.ElementId)
        {
            case "bank_btn_deposit":
                Token().Player.SendServerMessage("Deposit workflow is coming soon.", ColorConstants.White);
                break;
            case "bank_btn_withdraw":
                Token().Player.SendServerMessage("Withdraw workflow is coming soon.", ColorConstants.White);
                break;
            case "bank_btn_transfer":
                Token().Player.SendServerMessage("Transfer workflow is coming soon.", ColorConstants.White);
                break;
            case "bank_btn_confirm_deposit":
                Token().Player.SendServerMessage("Confirming deposits will be implemented in the next iteration.",
                    ColorConstants.White);
                break;
            case "bank_btn_view_history":
                Token().Player.SendServerMessage("Ledger history will be available in a future update.",
                    ColorConstants.White);
                break;
            case "bank_btn_close_account":
                Token().Player.SendServerMessage("Account closure requires banker assistance.",
                    ColorConstants.White);
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
    }

    public override void Close()
    {
        _token.Close();
    }
}
