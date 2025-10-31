using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
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
    private readonly IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> _accountQuery;

    public BankAccountModel(IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?> accountQuery)
    {
        _accountQuery = accountQuery;
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
