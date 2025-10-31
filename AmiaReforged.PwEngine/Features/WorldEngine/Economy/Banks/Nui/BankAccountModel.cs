using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks.Nui;

/// <summary>
/// Encapsulates the projection of a player's bank account for the Nui window.
/// Responsible for querying account data and exposing formatted values for binding.
/// </summary>
public sealed class BankAccountModel
{
    private const int DefaultPersonalDeposit = 500;
    private const int DefaultOrganizationDeposit = 500;

    private readonly IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> _accountQuery;
    private readonly IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> _eligibilityQuery;

    public BankAccountModel(
        IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> accountQuery,
        IQueryHandler<GetCoinhouseAccountEligibilityQuery, CoinhouseAccountEligibilityResult> eligibilityQuery)
    {
        _accountQuery = accountQuery;
        _eligibilityQuery = eligibilityQuery;
    }

    public string BankTitle { get; private set; } = "Coinhouse Banking";
    public PersonaId Persona { get; private set; } = default;
    public CoinhouseTag Coinhouse { get; private set; } = default;

    public bool AccountExists { get; private set; }
    public CoinhouseAccountSummary? AccountSummary { get; private set; }
    public int CurrentBalance { get; private set; }
    public DateTime? LastAccessedAt { get; private set; }

    public List<string> AccountEntries { get; } = new();
    public List<string> HoldingEntries { get; } = new();
    public List<string> DepositInventoryItems { get; } = new();
    public List<string> PendingDepositItems { get; } = new();
    public List<NuiComboEntry> DepositModeOptions { get; } = new();
    public List<NuiComboEntry> WithdrawModeOptions { get; } = new();
    public List<NuiComboEntry> OrganizationOptions { get; } = new();
    public List<OrganizationAccountEligibility> OrganizationEligibility { get; } = new();

    public CoinhouseAccountEligibilityResult? Eligibility { get; private set; }
    public string EligibilitySummary { get; private set; } = string.Empty;
    public string PersonalEligibilityStatus { get; private set; } = string.Empty;
    public string OrganizationEligibilityStatus { get; private set; } = string.Empty;
    public int SelectedOrganizationOption { get; set; }
    public bool CanOpenPersonalAccount => Eligibility?.CanOpenPersonalAccount ?? false;
    public bool HasOrganizationChoice => OrganizationEligibility.Count > 0;

    public int PersonalOpeningDeposit { get; private set; } = DefaultPersonalDeposit;
    public int OrganizationOpeningDeposit { get; private set; } = DefaultOrganizationDeposit;

    public int SelectedDepositMode { get; set; }
    public int SelectedWithdrawMode { get; set; }

    public string BalanceLabel => $"Current Balance: {FormatCurrency(CurrentBalance)}";
    public string LastAccessedDisplay => LastAccessedAt.HasValue
        ? $"Last Activity: {LastAccessedAt.Value.ToLocalTime():g}"
        : "Last Activity: not recorded";
    public string Subtitle => $"Coinhouse Tag: {Coinhouse.Value ?? string.Empty}";

    public void SetIdentity(PersonaId persona, CoinhouseTag coinhouse, string bankTitle)
    {
        Persona = persona;
        Coinhouse = coinhouse;
        BankTitle = string.IsNullOrWhiteSpace(bankTitle)
            ? $"Coinhouse ({coinhouse.Value})"
            : bankTitle;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(Persona.Value))
        {
            throw new InvalidOperationException("Persona must be assigned before loading account data.");
        }

        AccountEntries.Clear();
        HoldingEntries.Clear();
        DepositInventoryItems.Clear();
        PendingDepositItems.Clear();
        DepositModeOptions.Clear();
        WithdrawModeOptions.Clear();
        OrganizationOptions.Clear();
        OrganizationEligibility.Clear();
        Eligibility = null;
        EligibilitySummary = string.Empty;
        PersonalEligibilityStatus = string.Empty;
        OrganizationEligibilityStatus = string.Empty;
        SelectedOrganizationOption = 0;
    PersonalOpeningDeposit = DefaultPersonalDeposit;
    OrganizationOpeningDeposit = DefaultOrganizationDeposit;

        GetCoinhouseAccountQuery query = new(Persona, Coinhouse);
        CoinhouseAccountQueryResult? result = await _accountQuery.HandleAsync(query, cancellationToken);

        AccountExists = result?.AccountExists ?? false;
        AccountSummary = result?.Account;

        if (!AccountExists || AccountSummary is null)
        {
            CurrentBalance = 0;
            LastAccessedAt = null;

            AccountEntries.Add("No active account");
            HoldingEntries.Add("Open a treasury to begin banking.");

            await LoadEligibilityAsync(cancellationToken);

            SeedDefaultCombos();
            EnsureInventoryPlaceholder();
            return;
        }

        LastAccessedAt = AccountSummary.LastAccessedAt;

        int debit = AccountSummary.Debit;
        int credit = AccountSummary.Credit;
        CurrentBalance = debit - credit;

        AccountEntries.Add(BankTitle);

        HoldingEntries.Add($"Available Funds - {FormatCurrency(CurrentBalance)}");

        if (credit > 0)
        {
            HoldingEntries.Add($"Outstanding Debt - {FormatCurrency(credit)}");
        }

        if (debit > 0)
        {
            HoldingEntries.Add($"Deposited Assets - {FormatCurrency(debit)}");
        }

        SeedDefaultCombos();
        EnsureInventoryPlaceholder();
    }

    private async Task LoadEligibilityAsync(CancellationToken cancellationToken)
    {
        GetCoinhouseAccountEligibilityQuery eligibilityQuery = new(Persona, Coinhouse);
        CoinhouseAccountEligibilityResult eligibility =
            await _eligibilityQuery.HandleAsync(eligibilityQuery, cancellationToken);

        Eligibility = eligibility;

        if (eligibility.PersonalAccountOpeningDeposit > 0)
        {
            PersonalOpeningDeposit = eligibility.PersonalAccountOpeningDeposit;
        }

        if (!eligibility.CoinhouseExists)
        {
            EligibilitySummary = eligibility.CoinhouseError ?? "The selected coinhouse is unavailable.";
            PersonalEligibilityStatus = "Personal accounts cannot be opened right now.";
            OrganizationEligibilityStatus = string.Empty;
            OrganizationOptions.Add(new NuiComboEntry("No organizations available", 0));
            return;
        }

        EligibilitySummary = $"Opening an account requires an initial deposit of {FormatCurrency(PersonalOpeningDeposit)}.";

        PersonalEligibilityStatus = eligibility.CanOpenPersonalAccount
            ? $"You are eligible to open a personal account with {FormatCurrency(PersonalOpeningDeposit)}."
            : eligibility.PersonalAccountBlockedReason ?? "Personal account access is currently blocked.";

        if (eligibility.Organizations.Count == 0)
        {
            OrganizationEligibilityStatus = "You do not lead any organizations that can open an account here.";
            OrganizationOptions.Add(new NuiComboEntry("No organizations available", 0));
            return;
        }

        OrganizationEligibilityStatus =
            $"Select an organization to open a shared account (requires {FormatCurrency(OrganizationOpeningDeposit)}).";

        OrganizationEligibility.AddRange(eligibility.Organizations);

        int? firstDeposit = eligibility.Organizations.FirstOrDefault()?.RequiredDeposit;
        if (firstDeposit.HasValue && firstDeposit > 0)
        {
            OrganizationOpeningDeposit = firstDeposit.Value;
            OrganizationEligibilityStatus =
                $"Select an organization to open a shared account (requires {FormatCurrency(OrganizationOpeningDeposit)}).";
        }

        for (int index = 0; index < OrganizationEligibility.Count; index++)
        {
            OrganizationAccountEligibility option = OrganizationEligibility[index];
            string label = option.CanOpen
                ? option.OrganizationName
                : option.AlreadyHasAccount
                    ? $"{option.OrganizationName} (already has account)"
                    : $"{option.OrganizationName} (blocked)";

            OrganizationOptions.Add(new NuiComboEntry(label, index));
        }

        SelectedOrganizationOption = OrganizationEligibility.FindIndex(o => o.CanOpen);
        if (SelectedOrganizationOption < 0)
        {
            SelectedOrganizationOption = 0;
        }
    }

    public OrganizationAccountEligibility? GetOrganizationSelection(int selection)
    {
        if (OrganizationEligibility.Count == 0)
        {
            return null;
        }

        if (selection < 0 || selection >= OrganizationEligibility.Count)
        {
            return null;
        }

        return OrganizationEligibility[selection];
    }

    private static string FormatCurrency(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture) + " gp";
    }

    private void SeedDefaultCombos()
    {
        DepositModeOptions.Add(new NuiComboEntry("Gold Coins", 0));
        DepositModeOptions.Add(new NuiComboEntry("Inventory Item", 1));
        DepositModeOptions.Add(new NuiComboEntry("Letter of Credit", 2));

        WithdrawModeOptions.Add(new NuiComboEntry("Gold Coins", 0));
        WithdrawModeOptions.Add(new NuiComboEntry("Bank Draft", 1));
        WithdrawModeOptions.Add(new NuiComboEntry("Send Courier", 2));

        SelectedDepositMode = 0;
        SelectedWithdrawMode = 0;
    }

    private void EnsureInventoryPlaceholder()
    {
        if (DepositInventoryItems.Count == 0)
        {
            DepositInventoryItems.Add("No items are ready for deposit.");
        }

        if (PendingDepositItems.Count == 0)
        {
            PendingDepositItems.Add("No pending transactions.");
        }
    }
}
