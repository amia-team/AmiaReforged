using System.Globalization;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Access;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.UI.Banking;

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

    [Inject] private Lazy<IEconomySubsystem> Economy { get; init; } = null!;

    [Inject] private Lazy<IBankAccessEvaluator> BankAccessEvaluator { get; init; } = null!;

    [Inject] private Lazy<IForeclosureStorageService> ForeclosureStorageService { get; init; } = null!;

    [Inject] private Lazy<IPersonalStorageService> PersonalStorageService { get; init; } = null!;

    [Inject] private Lazy<IBankStorageItemBlacklist> StorageBlacklist { get; init; } = null!;

    [Inject] private WindowDirector WindowDirector { get; init; } = null!;

    private BankAccountModel Model => _model ??= new BankAccountModel(
        Economy.Value.Banking,
        BankAccessEvaluator.Value);

    public override BankWindowView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _model ??= new BankAccountModel(
            Economy.Value.Banking,
            BankAccessEvaluator.Value);

        _window = new NuiWindow(View.RootLayout(), _bankDisplayName)
        {
            Geometry = new NuiRect(
                BankWindowView.WindowPosX,
                BankWindowView.WindowPosY,
                BankWindowView.WindowWidth,
                BankWindowView.WindowHeight),
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

        // Set up watch for account selector changes
        Token().SetBindWatch(View.AccessibleAccountSelection, true);

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

        // Show account opening options when player doesn't have their own account
        // (they may still have access to shared accounts)
        string eligibilitySummary = Model.HasOwnAccount
            ? "You already have an active account at this coinhouse."
            : Model.EligibilitySummary;

        string personalStatus = Model.HasOwnAccount
            ? "Personal account is already open."
            : Model.PersonalEligibilityStatus;

        string organizationStatus = Model.HasOwnAccount
            ? "Shared account tools will become available after provisioning."
            : Model.OrganizationEligibilityStatus;

        Token().SetBindValue(View.EligibilitySummary, eligibilitySummary);
        Token().SetBindValue(View.PersonalEligibilityStatus, personalStatus);
        Token().SetBindValue(View.OrganizationEligibilityStatus, organizationStatus);

        bool showPersonalActions = !Model.HasOwnAccount;
        Token().SetBindValue(View.ShowPersonalAccountActions, showPersonalActions);
        Token().SetBindValue(View.HasActiveAccount, Model.AccountExists);

        // Update account selector (for switching between personal and shared accounts)
        Token().SetBindValue(View.AccessibleAccountEntries, Model.AccessibleAccountOptions);
        Token().SetBindValue(View.AccessibleAccountSelection, Model.SelectedAccountIndex);
        Token().SetBindValue(View.ShowAccountSelector, Model.HasMultipleAccounts);
        Token().SetBindValue(View.CurrentAccountLabel, Model.CurrentAccountRoleLabel);

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
        // Handle account selector changes
        if (obj.EventType == NuiEventType.Watch && obj.ElementId == "bank_accessible_account_selection")
        {
            _ = HandleAccountSelectionChangeAsync();
            return;
        }

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

    private async Task HandleAccountSelectionChangeAsync()
    {
        int selectedIndex = Token().GetBindValue(View.AccessibleAccountSelection);

        if (selectedIndex == Model.SelectedAccountIndex)
        {
            return; // No change
        }

        Token().Player.SendServerMessage($"Switching to selected account...", ColorConstants.White);

        try
        {
            await Model.SwitchToAccountAsync(selectedIndex);

            // Reload foreclosed items and personal storage for the new account context
            Guid playerKey = CharacterService.Value.GetPlayerKey(_player);
            await LoadForeclosedItemsAsync(playerKey);
            await LoadPersonalStorageAsync(playerKey);
            await LoadInventoryItemsAsync();

            await NwTask.SwitchToMainThread();

            UpdateView();
            ResetAmountInputs();

            string accountName = Model.CurrentAccount?.DisplayName ?? "account";
            Token().Player.SendServerMessage($"Now viewing: {accountName}", ColorConstants.Lime);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to switch accounts for player {PlayerName}", _player.PlayerName);
            Token().Player.SendServerMessage("Failed to switch accounts. Please try again.", ColorConstants.Orange);
        }
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
                openResult = await Economy.Value.Banking.OpenCoinhouseAccountAsync(command);
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

                    depositResult = await Economy.Value.Banking.DepositGoldAsync(depositCommand);
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

        // Provide feedback to player
        Token().Player.SendServerMessage(
            message: $"You pay {FormatCurrency(amount)}.",
            ColorConstants.Yellow);

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

            // Provide feedback to player
            Token().Player.SendServerMessage(
                message: $"You receive {FormatCurrency(amount)} back.",
                ColorConstants.Lime);
        }
    }

    private async Task GiveGoldAsync(int amount, bool silent = false)
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

            // Provide feedback to player (unless silent)
            if (!silent)
            {
                Token().Player.SendServerMessage(
                    message: $"You receive {FormatCurrency(amount)}.",
                    ColorConstants.Lime);
            }
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
            result = await Economy.Value.Banking.OpenCoinhouseAccountAsync(command);
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

                result = await Economy.Value.Banking.DepositGoldAsync(command);
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

            result = await Economy.Value.Banking.WithdrawGoldAsync(command);
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

        await GiveGoldAsync(amount, silent: true); // Give coins to the player silently.

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

            // Skip items blocked from storage (plot items, blacklisted resrefs)
            if (StorageBlacklist.Value.IsBlockedFromStorage(item))
            {
                continue;
            }

            _inventoryItems.Add(item);
        }

        // Populate model labels for depositable items
        Model.DepositInventoryItems.Clear();
        foreach (NwItem it in _inventoryItems)
        {
            string label = string.IsNullOrWhiteSpace(it.Name) ? it.ResRef : it.Name;
            Model.DepositInventoryItems.Add(label);
        }

        if (Model.DepositInventoryItems.Count == 0)
        {
            Model.DepositInventoryItems.Add("No items are ready for deposit.");
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

        // Double-check blocked items (defense in depth)
        if (StorageBlacklist.Value.IsBlockedFromStorage(item))
        {
            Token().Player.SendServerMessage(
                message: "That item cannot be stored in the bank.",
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
        WindowDirector.OpenWindow(storageWindow.Presenter);
    }

    private void OpenAdminWindow()
    {
        BankAdminWindowView adminWindow = new(_player, _coinhouseTag, _bankDisplayName);
        WindowDirector.OpenWindow(adminWindow.Presenter);
    }
}
