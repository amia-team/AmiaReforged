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

    // Bindings for adding account holders via target
    public readonly NuiBind<List<NuiComboEntry>> HolderRoleEntries = new("admin_holder_role_entries");
    public readonly NuiBind<int> NewHolderRoleSelection = new("admin_new_holder_role_selection");
    public readonly NuiBind<bool> TargetHolderVisible = new("admin_target_holder_visible");
    public readonly NuiBind<bool> TargetHolderEnabled = new("admin_target_holder_enabled");
    public readonly NuiBind<string> HolderStatusMessage = new("admin_holder_status_message");
    public readonly NuiBind<bool> HolderStatusVisible = new("admin_holder_status_visible");
    public readonly NuiBind<bool> CannotAddHolders = new("admin_cannot_add_holders");

    // Bindings for transaction history (placeholder)
    public readonly NuiBind<int> TransactionCount = new("admin_transaction_count");
    public readonly NuiBind<string> TransactionDates = new("admin_transaction_dates");
    public readonly NuiBind<string> TransactionDescriptions = new("admin_transaction_descriptions");
    public readonly NuiBind<string> TransactionAmounts = new("admin_transaction_amounts");

    // Buttons
    public NuiButton TargetHolderButton = null!;
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

                // Add Account Holder Section
                new NuiLabel("Add Account Holder")
                {
                    Height = 24f,
                    HorizontalAlign = NuiHAlign.Left,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(50, 40, 30)
                },
                new NuiRow
                {
                    Visible = TargetHolderVisible,
                    Height = 36f,
                    Children =
                    [
                        new NuiCombo
                        {
                            Id = "admin_holder_role_combo",
                            Entries = HolderRoleEntries,
                            Selected = NewHolderRoleSelection,
                            Width = 180f
                        },
                        new NuiSpacer { Width = 12f },
                        new NuiButton("Target Player")
                        {
                            Id = "admin_btn_target_holder",
                            Width = 120f,
                            Height = 30f,
                            Enabled = TargetHolderEnabled,
                            Tooltip = "Click to target a player character to add as an account holder."
                        }.Assign(out TargetHolderButton),
                        new NuiSpacer { Width = 12f },
                        new NuiLabel(HolderStatusMessage)
                        {
                            Visible = HolderStatusVisible,
                            Width = 240f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(50, 40, 30)
                        }
                    ]
                },
                new NuiLabel("Only account owners can add holders.")
                {
                    Visible = CannotAddHolders,
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
    private int _selectedNewHolderRole;
    private bool _canManageHolders;
    private Guid _requestorHolderId;

    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; } = null!;
    [Inject] private Lazy<Characters.Runtime.RuntimeCharacterService> CharacterService { get; init; } = null!;
    [Inject] private WindowDirector WindowDirector { get; init; } = null!;
    [Inject] private Lazy<IBankingFacade> BankingFacade { get; init; } = null!;
    [Inject] private ICommandHandler<JoinCoinhouseAccountCommand> JoinAccountHandler { get; init; } = null!;

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
        InitializeHolderRoles();
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

    private void InitializeHolderRoles()
    {
        List<NuiComboEntry> roleEntries =
        [
            new("Joint Owner (Full Access)", (int)HolderRole.JointOwner),
            new("Authorized User (Transactions)", (int)HolderRole.AuthorizedUser),
            new("Trustee (View Only)", (int)HolderRole.Trustee),
        ];

        Token().SetBindValue(View.HolderRoleEntries, roleEntries);
        Token().SetBindValue(View.NewHolderRoleSelection, (int)HolderRole.JointOwner);
        Token().SetBindValue(View.HolderStatusMessage, "");
        Token().SetBindValue(View.HolderStatusVisible, false);
        _selectedNewHolderRole = (int)HolderRole.JointOwner;
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
                _accessProfile = BankAccessProfile.None;
                UpdateAccountHolderDisplay([]);
                UpdateTransactionDisplay([]);
                UpdateAddHolderAccessState(canAdd: false);
                return;
            }

            // Evaluate access profile for the current user
            CoinhouseAccountSummary summary = new()
            {
                CoinhouseId = 0, // Not needed for permission evaluation
                CoinhouseTag = _coinhouseTag,
                Debit = 0,
                Credit = 0,
                OpenedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };
            _accessProfile = BankAccessEvaluator.Value.Evaluate(_persona, summary, _accountData.Holders);

            // Determine if the current user can manage holders
            CoinhouseAccountHolderDto? currentUserHolder = _accountData.Holders
                .FirstOrDefault(h => h.HolderId == _requestorHolderId);

            _canManageHolders = currentUserHolder?.Role is HolderRole.Owner or HolderRole.JointOwner;

            UpdateAccountHolderDisplay(_accountData.Holders);
            UpdateTransactionDisplay([]);
            UpdateAddHolderAccessState(canAdd: _accessProfile.CanIssueShares);
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

    private void UpdateAddHolderAccessState(bool canAdd)
    {
        Token().SetBindValue(View.TargetHolderVisible, canAdd);
        Token().SetBindValue(View.TargetHolderEnabled, canAdd);
        Token().SetBindValue(View.CannotAddHolders, !canAdd);
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

                case "admin_btn_target_holder":
                    BeginTargetHolder();
                    break;

                case "admin_btn_remove_holder":
                    _ = HandleRemoveHolderAsync(e.ArrayIndex);
                    break;

                case "admin_holder_role_combo":
                    HandleHolderRoleChange();
                    break;
            }
        }
    }

    private void HandleHolderRoleChange()
    {
        int selected = Token().GetBindValue(View.NewHolderRoleSelection);
        _selectedNewHolderRole = selected;
    }

    private void BeginTargetHolder()
    {
        if (_accountData == null || !_accountData.AccountExists)
        {
            return;
        }

        if (_accessProfile?.CanIssueShares != true)
        {
            Token().SetBindValue(View.HolderStatusMessage, "Only account owners can add holders.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        Token().SetBindValue(View.HolderStatusMessage, "Target a player character to add as holder...");
        Token().SetBindValue(View.HolderStatusVisible, true);

        _player.EnterTargetMode(HandleAddHolderTarget, new TargetModeSettings
        {
            CursorType = MouseCursor.Action,
            ValidTargets = ObjectTypes.Creature
        });
    }

    private void HandleAddHolderTarget(ModuleEvents.OnPlayerTarget targetData)
    {
        if (targetData.TargetObject is not NwCreature targetCreature)
        {
            Token().SetBindValue(View.HolderStatusMessage, "You must target a player character.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        NwPlayer? targetPlayer = targetCreature.ControllingPlayer;
        if (targetPlayer == null || !targetCreature.IsPlayerControlled)
        {
            Token().SetBindValue(View.HolderStatusMessage, "Target must be a player character.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        if (!CharacterService.Value.TryGetPlayerKey(targetPlayer, out Guid targetCharacterId))
        {
            Token().SetBindValue(View.HolderStatusMessage, "Unable to determine target character.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        PersonaId targetPersona = PersonaId.FromCharacter(new CharacterId(targetCharacterId));
        string memberName = targetCreature.Name;

        // Check if the target is the same player
        if (targetCharacterId == _requestorHolderId)
        {
            Token().SetBindValue(View.HolderStatusMessage, "You cannot add yourself as a holder.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        // Check if already a holder
        if (_holders.Any(h => h.HolderId == targetCharacterId))
        {
            Token().SetBindValue(View.HolderStatusMessage, $"{memberName} is already an account holder.");
            Token().SetBindValue(View.HolderStatusVisible, true);
            return;
        }

        _ = AddHolderAsync(targetPersona, memberName);
    }

    private async Task AddHolderAsync(PersonaId holderPersona, string holderName)
    {
        if (_accountData == null)
        {
            return;
        }

        HolderRole role = (HolderRole)_selectedNewHolderRole;
        
        // Map HolderRole to BankShareType
        BankShareType shareType = role switch
        {
            HolderRole.JointOwner => BankShareType.JointOwner,
            HolderRole.AuthorizedUser => BankShareType.AuthorizedUser,
            HolderRole.Trustee => BankShareType.Trustee,
            _ => BankShareType.AuthorizedUser
        };

        // Parse the holder name into first/last
        string[] nameParts = holderName.Split(' ', 2);
        string firstName = nameParts.Length > 0 ? nameParts[0] : holderName;
        string lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;
        
        JoinCoinhouseAccountCommand command = new(
            Requestor: _persona,
            AccountId: _accountData.AccountId,
            Coinhouse: _coinhouseTag,
            ShareType: shareType,
            HolderType: HolderType.Individual,
            Role: role,
            HolderFirstName: firstName,
            HolderLastName: lastName,
            NewHolder: holderPersona
        );

        try
        {
            CommandResult result = await JoinAccountHandler.HandleAsync(command);

            await NwTask.SwitchToMainThread();

            if (!result.Success)
            {
                Token().SetBindValue(View.HolderStatusMessage, result.ErrorMessage ?? "Failed to add holder.");
                Token().SetBindValue(View.HolderStatusVisible, true);
                return;
            }

            string roleName = FormatHolderRole(role);
            Token().SetBindValue(View.HolderStatusMessage, $"{holderName} added as {roleName}.");
            Token().SetBindValue(View.HolderStatusVisible, true);

            _player.SendServerMessage($"{holderName} has been added to your account as {roleName}.", ColorConstants.Green);

            // Reload account data to refresh the holder list
            await LoadAccountDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add holder to account at coinhouse {Coinhouse}", _coinhouseTag.Value);

            await NwTask.SwitchToMainThread();
            Token().SetBindValue(View.HolderStatusMessage, "An error occurred. Please try again.");
            Token().SetBindValue(View.HolderStatusVisible, true);
        }
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
}
