using System.Globalization;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking;

/// <summary>
/// Bank admin window for account management: account holders, transaction history, and share documents.
/// </summary>
public sealed class BankAdminWindowView : ScryView<BankAdminWindowPresenter>
{
    // Window dimensions
    public const float WindowWidth = 640f;
    public const float WindowHeight = 700f;
    private const float ContentWidth = WindowWidth - 40f;
    private const float StandardButtonWidth = 120f;
    private const float StandardButtonHeight = 32f;

    // Bindings for account holder display
    public readonly NuiBind<int> AccountHolderCount = new("admin_holder_count");
    public readonly NuiBind<string> HolderNames = new("admin_holder_names");
    public readonly NuiBind<string> HolderRoles = new("admin_holder_roles");
    public readonly NuiBind<string> HolderJoinedDates = new("admin_holder_joined_dates");

    // Bindings for holder management
    public readonly NuiBind<bool> CanManageHolders = new("admin_can_manage_holders");
    public readonly NuiBind<bool> HolderIsRemovable = new("admin_holder_is_removable");
    public readonly NuiBind<List<NuiComboEntry>> HolderRoleOptions = new("admin_holder_role_options");
    public readonly NuiBind<int> HolderRoleSelection = new("admin_holder_role_selection");

    // Bindings for share document issuance
    public readonly NuiBind<List<NuiComboEntry>> ShareTypeEntries = new("admin_share_type_entries");
    public readonly NuiBind<int> ShareTypeSelection = new("admin_share_type_selection");
    public readonly NuiBind<string> ShareInstructions = new("admin_share_instructions");
    public readonly NuiBind<bool> CanIssueShares = new("admin_can_issue_shares");
    public readonly NuiBind<bool> CannotIssueShares = new("admin_cannot_issue_shares");

    // Bindings for transaction history (placeholder)
    public readonly NuiBind<int> TransactionCount = new("admin_transaction_count");
    public readonly NuiBind<string> TransactionDates = new("admin_transaction_dates");
    public readonly NuiBind<string> TransactionDescriptions = new("admin_transaction_descriptions");
    public readonly NuiBind<string> TransactionAmounts = new("admin_transaction_amounts");

    // Buttons
    public NuiButton IssueShareDocumentButton = null!;
    public NuiButton RefreshButton = null!;
    public NuiButton CloseButton = null!;
    public NuiButton RemoveHolderButton = null!;

    public override BankAdminWindowPresenter Presenter { get; protected set; }

    public BankAdminWindowView(NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        Presenter = new BankAdminWindowPresenter(this, player, coinhouseTag, bankDisplayName);

        InjectionService injector = AnvilCore.GetService<InjectionService>()!;
        injector.Inject(Presenter);
    }

    public override NuiLayout RootLayout()
    {
        // Account holders list template with management controls
        List<NuiListTemplateCell> holderRowTemplate =
        [
            new(new NuiLabel(HolderNames) { Width = 180f, HorizontalAlign = NuiHAlign.Left }),
            new(new NuiSpacer { Width = 4f }),
            new(new NuiLabel(HolderRoles) { Width = 140f, HorizontalAlign = NuiHAlign.Left }),
            new(new NuiSpacer { Width = 4f }),
            new(new NuiButton("X")
            {
                Id = "admin_btn_remove_holder",
                Width = 28f,
                Height = 24f,
                Tooltip = new NuiBind<string>("admin_remove_tooltip"),
                Enabled = HolderIsRemovable
            }.Assign(out RemoveHolderButton))
        ];

        // Transaction history list template (placeholder)
        List<NuiListTemplateCell> transactionRowTemplate =
        [
            new(new NuiLabel(TransactionDates) { Width = 120f, HorizontalAlign = NuiHAlign.Left }),
            new(new NuiSpacer { Width = 8f }),
            new(new NuiLabel(TransactionDescriptions) { Width = 280f, HorizontalAlign = NuiHAlign.Left }),
            new(new NuiSpacer { Width = 8f }),
            new(new NuiLabel(TransactionAmounts) { Width = 100f, HorizontalAlign = NuiHAlign.Right })
        ];

        return new NuiColumn
        {
            Children =
            [
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 700f, 700f))]
                },
                // Account Holders Section
                new NuiRow
                {
                    Children =
                    [
                        new NuiLabel("Account Holders")
                        {
                            Height = 28f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        },
                        new NuiSpacer(),
                        new NuiButton("Refresh")
                        {
                            Id = "admin_btn_refresh",
                            Width = 100f,
                            Height = 28f
                        }.Assign(out RefreshButton)
                    ]
                },
                new NuiList(holderRowTemplate, AccountHolderCount)
                {
                    RowHeight = 28f,
                    Height = 180f
                },
                new NuiSpacer { Height = 12f },

                // Share Document Issuance Section
                new NuiLabel("Issue Share Document")
                {
                    Height = 24f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiRow
                {
                    Visible = CanIssueShares,
                    Children =
                    [
                        new NuiCombo
                        {
                            Id = "admin_share_combo",
                            Entries = ShareTypeEntries,
                            Selected = ShareTypeSelection
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton("Issue Document")
                        {
                            Id = "admin_btn_issue_share",
                            Width = 180f,
                            Height = 32f
                        }.Assign(out IssueShareDocumentButton)
                    ]
                },
                new NuiLabel(ShareInstructions)
                {
                    Visible = CanIssueShares,
                    Height = 44f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Top,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiLabel("Only account owners can issue share documents.")
                {
                    Visible = CannotIssueShares,
                    Height = 32f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiSpacer { Height = 12f },

                // Transaction History Section (placeholder)
                new NuiLabel("Recent Transactions")
                {
                    Height = 24f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiList(transactionRowTemplate, TransactionCount)
                {
                    RowHeight = 28f,
                    Height = 180f
                },
                new NuiLabel("Transaction history will be available in a future update.")
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiSpacer(),

                // Footer buttons
                new NuiRow
                {
                    Children =
                    [
                        new NuiSpacer(),
                        new NuiButton("Close")
                        {
                            Id = "admin_btn_close",
                            Width = StandardButtonWidth,
                            Height = StandardButtonHeight
                        }.Assign(out CloseButton),
                        new NuiSpacer()
                    ]
                }
            ]
        };
    }
}

public sealed class BankAdminWindowPresenter : ScryPresenter<BankAdminWindowView>
{
    private const string ShareDocumentResRef = "bank_sharedoc";
    private const string ShareDocumentFallbackResRef = "nw_it_mp_scroll001";

    private static class ShareDocumentLocals
    {
        public const string AccountId = "bank_share_account_id";
        public const string CoinhouseTag = "bank_share_coinhouse_tag";
        public const string ShareType = "bank_share_type";
        public const string ShareTypeId = "bank_share_type_id";
        public const string HolderRole = "bank_share_holder_role";
        public const string HolderRoleId = "bank_share_holder_role_id";
        public const string Issuer = "bank_share_issuer";
        public const string DocumentId = "bank_share_document_id";
        public const string IssuedAt = "bank_share_issued_at";
        public const string BankName = "bank_share_bank_name";
    }

    private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly CoinhouseTag _coinhouseTag;
    private readonly string _bankDisplayName;
    private PersonaId _persona;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private CoinhouseAccountQueryResult? _accountData;
    private List<CoinhouseAccountHolderDto> _holders = [];
    private BankAccessProfile? _accessProfile;
    private int _selectedShareType;
    private bool _canManageHolders;
    private Guid _requestorHolderId;

    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; } = null!;
    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;
    [Inject] private WindowDirector WindowDirector { get; init; } = null!;
    [Inject] private Lazy<IBankingFacade> BankingFacade { get; init; } = null!;

    public BankAdminWindowPresenter(BankAdminWindowView view, NwPlayer player, CoinhouseTag coinhouseTag, string bankDisplayName)
    {
        View = view;
        _player = player;
        _coinhouseTag = coinhouseTag;
        _bankDisplayName = bankDisplayName;
    }

    public override BankAdminWindowView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        // Initialize persona after dependency injection
        Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
        if (playerKey == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Cannot open admin window: Player character does not have a UUID (character not registered).");
        }

        CharacterId characterId = CharacterId.From(playerKey);
        _persona = PersonaId.FromCharacter(characterId);
        _requestorHolderId = playerKey;

        _window = new NuiWindow(View.RootLayout(), $"{_bankDisplayName} - Account Management")
        {
            Geometry = new NuiRect(140f, 140f, BankAdminWindowView.WindowWidth, BankAdminWindowView.WindowHeight),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null) InitBefore();
        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);

        SubscribeEvents();
        InitializeShareTypes();
        _ = LoadAccountDataAsync();
    }

    public override void Close()
    {
        _token.Close();
    }

    private void SubscribeEvents()
    {
        Token().OnNuiEvent += HandleNuiEvent;
    }

    private void InitializeShareTypes()
    {
        List<NuiComboEntry> shareEntries =
        [
            new("Joint Owner (Full Access)", (int)BankShareType.JointOwner),
            new("Authorized User (Transactions)", (int)BankShareType.AuthorizedUser),
        ];

        Token().SetBindValue(View.ShareTypeEntries, shareEntries);
        Token().SetBindValue(View.ShareTypeSelection, 0);
        _selectedShareType = (int)BankShareType.JointOwner;
        UpdateShareInstructions(BankShareType.JointOwner);
    }

    private async Task LoadAccountDataAsync()
    {
        try
        {
            // Query account data using the banking facade
            GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
            _accountData = await BankingFacade.Value.GetCoinhouseAccountAsync(query);

            await NwTask.SwitchToMainThread();

            if (_accountData == null || !_accountData.AccountExists)
            {
                _canManageHolders = false;
                UpdateAccountHolderDisplay([]);
                UpdateTransactionDisplay([]);
                UpdateShareAccessState(canIssue: false);
                return;
            }

            // Determine if the current user can manage holders
            CoinhouseAccountHolderDto? currentUserHolder = _accountData.Holders
                .FirstOrDefault(h => h.HolderId == _requestorHolderId);

            _canManageHolders = currentUserHolder?.Role is HolderRole.Owner or HolderRole.JointOwner;
            bool canIssueShares = currentUserHolder?.Role is HolderRole.Owner or HolderRole.JointOwner;

            UpdateAccountHolderDisplay(_accountData.Holders);
            UpdateTransactionDisplay([]);
            UpdateShareAccessState(canIssue: canIssueShares);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load account data for admin window at coinhouse {Coinhouse}", _coinhouseTag.Value);

            await NwTask.SwitchToMainThread();
            _player.SendServerMessage("Failed to load account information. Please try again.", ColorConstants.Orange);
        }
    }

    private void UpdateAccountHolderDisplay(IReadOnlyList<CoinhouseAccountHolderDto> holders)
    {
        _holders = holders.ToList();

        List<string> names = [];
        List<string> roles = [];
        List<bool> isRemovable = [];
        List<int> roleSelections = [];
        List<string> removeTooltips = [];

        // Build role options for the combo box (excludes Owner role)
        List<NuiComboEntry> roleOptions =
        [
            new("Joint Owner", 0),
            new("Authorized User", 1),
            new("Trustee", 2),
            new("Viewer", 3)
        ];

        foreach (CoinhouseAccountHolderDto holder in holders)
        {
            string fullName = $"{holder.FirstName} {holder.LastName}".Trim();
            names.Add(string.IsNullOrEmpty(fullName) ? "Unknown" : fullName);
            roles.Add(FormatHolderRole(holder.Role));

            // Owner cannot be removed or have role changed
            bool canRemove = _canManageHolders && holder.Role != HolderRole.Owner;
            isRemovable.Add(canRemove);
            roleSelections.Add(GetComboIndexFromRole(holder.Role));

            if (holder.Role == HolderRole.Owner)
            {
                removeTooltips.Add("The account owner cannot be removed");
            }
            else if (!_canManageHolders)
            {
                removeTooltips.Add("You do not have permission to manage holders");
            }
            else
            {
                removeTooltips.Add("Remove this holder from the account");
            }
        }

        Token().SetBindValues(View.HolderNames, names);
        Token().SetBindValues(View.HolderRoles, roles);
        Token().SetBindValues(View.HolderIsRemovable, isRemovable);
        Token().SetBindValues(View.HolderRoleSelection, roleSelections);
        Token().SetBindValue(View.HolderRoleOptions, roleOptions);
        Token().SetBindValue(View.CanManageHolders, _canManageHolders);
        Token().SetBindValue(View.AccountHolderCount, holders.Count);
    }

    private void UpdateTransactionDisplay(IReadOnlyList<object> transactions)
    {
        // Placeholder for transaction history
        // This will be implemented when transaction logging is added
        Token().SetBindValues(View.TransactionDates, new List<string>());
        Token().SetBindValues(View.TransactionDescriptions, new List<string>());
        Token().SetBindValues(View.TransactionAmounts, new List<string>());
        Token().SetBindValue(View.TransactionCount, 0);
    }

    private void UpdateShareAccessState(bool canIssue)
    {
        Token().SetBindValue(View.CanIssueShares, canIssue);
        Token().SetBindValue(View.CannotIssueShares, !canIssue);
    }

    private string FormatHolderRole(HolderRole role)
    {
        return role switch
        {
            HolderRole.Owner => "Owner",
            HolderRole.JointOwner => "Joint Owner",
            HolderRole.AuthorizedUser => "Authorized User",
            HolderRole.Trustee => "Trustee",
            _ => "Unknown"
        };
    }

    private void UpdateShareInstructions(BankShareType shareType)
    {
        string instructions = shareType switch
        {
            BankShareType.JointOwner => "Joint Owners have full account access including issuing shares.",
            BankShareType.AuthorizedUser => "Authorized Users can deposit, withdraw, and view balance.",
            BankShareType.Trustee => "Trustees can manage account on behalf of the owner.",
            _ => "Select a share type to see permissions."
        };

        Token().SetBindValue(View.ShareInstructions, instructions);
    }

    private void HandleNuiEvent(ModuleEvents.OnNuiEvent e)
    {
        if (e.EventType == NuiEventType.Click)
        {
            switch (e.ElementId)
            {
                case "admin_btn_close":
                    Close();
                    break;

                case "admin_btn_refresh":
                    _ = LoadAccountDataAsync();
                    break;

                case "admin_btn_issue_share":
                    _ = HandleIssueShareDocumentAsync();
                    break;

                case "admin_btn_remove_holder":
                    _ = HandleRemoveHolderAsync(e.ArrayIndex);
                    break;

                case "admin_share_combo":
                    HandleShareTypeChange();
                    break;
            }
        }
    }

    private void HandleShareTypeChange()
    {
        int selected = Token().GetBindValue(View.ShareTypeSelection);
        _selectedShareType = selected;

        BankShareType shareType = selected switch
        {
            0 => BankShareType.JointOwner,
            1 => BankShareType.AuthorizedUser,
            _ => BankShareType.JointOwner
        };

        UpdateShareInstructions(shareType);
    }

    private async Task HandleRemoveHolderAsync(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _holders.Count)
        {
            return;
        }

        CoinhouseAccountHolderDto holderToRemove = _holders[rowIndex];

        // Prevent removing the owner
        if (holderToRemove.Role == HolderRole.Owner)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "The account owner cannot be removed.",
                ColorConstants.Orange);
            return;
        }

        if (!_canManageHolders)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "You do not have permission to remove account holders.",
                ColorConstants.Orange);
            return;
        }

        if (_accountData == null || !_accountData.AccountExists)
        {
            return;
        }

        RemoveCoinhouseAccountHolderCommand command = new(
            _persona,
            _accountData.AccountId,
            _coinhouseTag,
            holderToRemove.HolderId);

        CommandResult result = await BankingFacade.Value.RemoveAccountHolderAsync(command);

        await NwTask.SwitchToMainThread();

        if (!result.Success)
        {
            _player.SendServerMessage(
                result.ErrorMessage ?? "Failed to remove account holder.",
                ColorConstants.Orange);
            return;
        }

        string holderName = $"{holderToRemove.FirstName} {holderToRemove.LastName}".Trim();
        _player.SendServerMessage(
            $"{holderName} has been removed from the account.",
            ColorConstants.White);

        // Refresh the holder list
        _ = LoadAccountDataAsync();
    }

    private async Task HandleHolderRoleChangeAsync(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _holders.Count)
        {
            return;
        }

        CoinhouseAccountHolderDto holderToUpdate = _holders[rowIndex];

        // Cannot change the owner's role
        if (holderToUpdate.Role == HolderRole.Owner)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "The account owner's role cannot be changed.",
                ColorConstants.Orange);
            return;
        }

        if (!_canManageHolders)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "You do not have permission to change account holder roles.",
                ColorConstants.Orange);
            return;
        }

        if (_accountData == null || !_accountData.AccountExists)
        {
            return;
        }

        // Get the selected new role from the combo
        List<int> selections = Token().GetBindValues(View.HolderRoleSelection);
        if (rowIndex >= selections.Count)
        {
            return;
        }

        int selectedRoleIndex = selections[rowIndex];
        HolderRole newRole = GetRoleFromComboIndex(selectedRoleIndex);

        if (newRole == holderToUpdate.Role)
        {
            return; // No change
        }

        UpdateCoinhouseAccountHolderRoleCommand command = new(
            _persona,
            _accountData.AccountId,
            _coinhouseTag,
            holderToUpdate.HolderId,
            newRole);

        CommandResult result = await BankingFacade.Value.UpdateAccountHolderRoleAsync(command);

        await NwTask.SwitchToMainThread();

        if (!result.Success)
        {
            _player.SendServerMessage(
                result.ErrorMessage ?? "Failed to update account holder role.",
                ColorConstants.Orange);
            // Reset the combo to the original role
            _ = LoadAccountDataAsync();
            return;
        }

        string holderName = $"{holderToUpdate.FirstName} {holderToUpdate.LastName}".Trim();
        _player.SendServerMessage(
            $"{holderName}'s role has been updated to {FormatHolderRole(newRole)}.",
            ColorConstants.White);

        // Refresh the holder list
        _ = LoadAccountDataAsync();
    }

    private static HolderRole GetRoleFromComboIndex(int index)
    {
        return index switch
        {
            0 => HolderRole.JointOwner,
            1 => HolderRole.AuthorizedUser,
            2 => HolderRole.Trustee,
            3 => HolderRole.Viewer,
            _ => HolderRole.Viewer
        };
    }

    private static int GetComboIndexFromRole(HolderRole role)
    {
        return role switch
        {
            HolderRole.JointOwner => 0,
            HolderRole.AuthorizedUser => 1,
            HolderRole.Trustee => 2,
            HolderRole.Viewer => 3,
            _ => 3
        };
    }

    private async Task HandleIssueShareDocumentAsync()
    {
        if (_accountData == null || !_accountData.AccountExists)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "Open an account before issuing share documents.",
                ColorConstants.White);
            return;
        }

        if (_accessProfile?.CanIssueShares != true)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "You are not authorized to issue share documents for this account.",
                ColorConstants.White);
            return;
        }

        BankShareType shareType = _selectedShareType switch
        {
            0 => BankShareType.JointOwner,
            1 => BankShareType.AuthorizedUser,
            2 => BankShareType.Trustee,
            _ => BankShareType.JointOwner
        };

        if (_player.LoginCreature is null)
        {
            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "You must be possessing a character to issue share documents.",
                ColorConstants.Orange);
            return;
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(_persona, _coinhouseTag);
        Guid documentId = Guid.NewGuid();
        HolderRole holderRole = shareType.ToHolderRole();
        DateTime issuedAt = DateTime.UtcNow;

        try
        {
            await NwTask.SwitchToMainThread();

            NwCreature? creature = _player.LoginCreature;
            if (creature?.Location == null)
            {
                _player.SendServerMessage(
                    "Share documents cannot be issued right now. Please try again shortly.",
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
                _player.SendServerMessage(
                    "The bank cannot produce share documents at this time.",
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
            NWScript.SetLocalString(document, ShareDocumentLocals.CoinhouseTag, _coinhouseTag.Value ?? string.Empty);
            NWScript.SetLocalString(document, ShareDocumentLocals.ShareType, shareType.ToString());
            NWScript.SetLocalInt(document, ShareDocumentLocals.ShareTypeId, (int)shareType);
            NWScript.SetLocalString(document, ShareDocumentLocals.HolderRole, holderRole.ToString());
            NWScript.SetLocalInt(document, ShareDocumentLocals.HolderRoleId, (int)holderRole);
            NWScript.SetLocalString(document, ShareDocumentLocals.Issuer, issuerName);
            NWScript.SetLocalString(document, ShareDocumentLocals.DocumentId, documentId.ToString());
            NWScript.SetLocalString(document, ShareDocumentLocals.IssuedAt,
                issuedAt.ToString("o", CultureInfo.InvariantCulture));
            NWScript.SetLocalString(document, ShareDocumentLocals.BankName, _bankDisplayName);

            creature.AcquireItem(document);

            _player.SendServerMessage(
                $"A {documentName} has been added to your inventory.",
                ColorConstants.White);

            Log.Info(
                "Issued bank share document {DocumentId} for account {AccountId} ({ShareType}) at coinhouse {Coinhouse}.",
                documentId, accountId, shareType, _coinhouseTag.Value ?? "(unknown)");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create share document for account at coinhouse {Coinhouse}.",
                _coinhouseTag.Value ?? "(unknown)");

            await NwTask.SwitchToMainThread();
            _player.SendServerMessage(
                "The banker failed to prepare the share document. Please try again later.",
                ColorConstants.Orange);
        }
    }

    private string FormatShareDocumentName(BankShareType shareType)
    {
        string roleName = shareType switch
        {
            BankShareType.JointOwner => "Joint Owner",
            BankShareType.AuthorizedUser => "Authorized User",
            BankShareType.Trustee => "Trustee",
            _ => "Unknown"
        };
        return $"{_bankDisplayName} Share ({roleName})";
    }

    private string BuildShareDocumentDescription(
        BankShareType shareType,
        Guid accountId,
        Guid documentId,
        string issuerName,
        DateTime issuedAt)
    {
        string roleName = shareType switch
        {
            BankShareType.JointOwner => "Joint Owner",
            BankShareType.AuthorizedUser => "Authorized User",
            BankShareType.Trustee => "Trustee",
            _ => "Unknown"
        };

        string summary = shareType switch
        {
            BankShareType.JointOwner => "Full account access including share issuance",
            BankShareType.AuthorizedUser => "Deposit, withdraw, and view balance",
            BankShareType.Trustee => "Manage account on behalf of owner",
            _ => "Unknown permissions"
        };

        string coinhouseTag = _coinhouseTag.Value ?? "Unspecified";

        return
            $"{_bankDisplayName}\nShare Role: {roleName}\nScope: {summary}\nCoinhouse: {coinhouseTag}\nAccount Reference: {accountId}\nDocument: {documentId}\nIssuer: {issuerName}\nIssued (UTC): {issuedAt.ToString("g", CultureInfo.InvariantCulture)}\n\nPresent this parchment at the banker to register the share.";
    }
}
