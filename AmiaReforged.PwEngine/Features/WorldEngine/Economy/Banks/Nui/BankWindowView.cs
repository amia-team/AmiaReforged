using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
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
    public readonly NuiBind<string> EligibilitySummary = new("bank_eligibility_summary");
    public readonly NuiBind<string> PersonalEligibilityStatus = new("bank_personal_eligibility_status");
    public readonly NuiBind<string> OrganizationEligibilityStatus = new("bank_organization_eligibility_status");
    public readonly NuiBind<List<NuiComboEntry>> OrganizationAccountEntries = new("bank_org_account_entries");
    public readonly NuiBind<int> OrganizationAccountSelection = new("bank_org_account_selection");

    public NuiButton DepositButton = null!;
    public NuiButton WithdrawButton = null!;
    public NuiButton TransferButton = null!;
    public NuiButton ConfirmDepositButton = null!;
    public NuiButton ViewHistoryButton = null!;
    public NuiButton CloseAccountButton = null!;
    public NuiButton DoneButton = null!;
    public NuiButton CancelButton = null!;
    public NuiButton HelpButton = null!;
    public NuiButton OpenPersonalAccountButton = null!;
    public NuiButton OpenOrganizationAccountButton = null!;

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
                new NuiSpacer { Height = 4f },
                BuildProvisioningSection(),
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

    private NuiElement BuildProvisioningSection()
    {
        return new NuiColumn
        {
            Children =
            [
                new NuiLabel(EligibilitySummary)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiButton("Open Personal Account")
                        {
                            Id = "bank_btn_open_personal",
                            Width = 200f,
                            Height = 32f
                        }.Assign(out OpenPersonalAccountButton),
                        new NuiSpacer { Width = 12f },
                        new NuiLabel(PersonalEligibilityStatus)
                        {
                            Width = 360f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle
                        }
                    ]
                },
                new NuiSpacer { Height = 2f },
                new NuiRow
                {
                    Height = 36f,
                    Children =
                    [
                        new NuiCombo
                        {
                            Id = "bank_org_account_combo",
                            Width = 260f,
                            Entries = OrganizationAccountEntries,
                            Selected = OrganizationAccountSelection
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton("Open Organization Account")
                        {
                            Id = "bank_btn_open_org",
                            Width = 220f,
                            Height = 32f
                        }.Assign(out OpenOrganizationAccountButton)
                    ]
                },
                new NuiLabel(OrganizationEligibilityStatus)
                {
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle
                }
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

    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;

    [Inject]
    private Lazy<IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>> AccountQueryHandler
    {
        get;
        init;
    }
        = null!;

    [Inject]
    private Lazy<IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult>>
        EligibilityQueryHandler { get; init; }
        = null!;

    [Inject]
    private Lazy<ICommandHandler<OpenCoinhouseAccountCommand>> OpenAccountCommandHandler { get; init; } = null!;

    [Inject]
    private Lazy<ICommandHandler<DepositGoldCommand>> DepositCommandHandler { get; init; } = null!;

    [Inject]
    private WindowDirector WindowDirector { get; init; } = null!;

    private BankAccountModel Model => _model ??= new BankAccountModel(
        AccountQueryHandler.Value,
        EligibilityQueryHandler.Value);

    public override BankWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _model ??= new BankAccountModel(AccountQueryHandler.Value, EligibilityQueryHandler.Value);

        _window = new NuiWindow(View.RootLayout(), _bankDisplayName)
        {
            Geometry = new NuiRect(120f, 120f, 720f, 980f),
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
            case "bank_btn_open_personal":
                _ = HandleOpenPersonalAccountAsync();
                break;
            case "bank_btn_open_org":
                _ = HandleOpenOrganizationAccountAsync();
                break;
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
        if (!await HasSufficientFundsAsync(deposit))
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
                goldDeducted = await TryWithdrawGoldAsync(deposit);
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

    private async Task<bool> HasSufficientFundsAsync(int required)
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
                message: "You must be possessing a character to open a coinhouse account.",
                ColorConstants.Orange);
            return false;
        }

        uint available = creature.Gold;
        uint requiredGold = (uint)Math.Max(required, 0);

        if (available < requiredGold)
        {
            Token().Player.SendServerMessage(
                message: $"You need {FormatCurrency(required)} on hand to open this account.",
                ColorConstants.Orange);
            return false;
        }

        return true;
    }

    private async Task<bool> TryWithdrawGoldAsync(int amount)
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
                message: "You must be possessing a character to open a coinhouse account.",
                ColorConstants.Orange);
            return false;
        }

        uint available = creature.Gold;
        uint requested = (uint)Math.Max(amount, 0);

        if (available < requested)
        {
            Token().Player.SendServerMessage(
                message: $"You need {FormatCurrency(amount)} on hand to open this account.",
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

    private async Task ReloadModelAsync()
    {
        try
        {
            await Model.LoadAsync();
            await NwTask.SwitchToMainThread();
            UpdateView();
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
}
