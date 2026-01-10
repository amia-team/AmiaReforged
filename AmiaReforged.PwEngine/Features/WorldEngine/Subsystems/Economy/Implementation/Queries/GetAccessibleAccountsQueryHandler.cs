using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

/// <summary>
/// Returns all accounts a persona can access at a given coinhouse.
/// This includes their own account (if any) plus shared accounts.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetAccessibleAccountsQuery, AccessibleAccountsResult>))]
public sealed class GetAccessibleAccountsQueryHandler 
    : IQueryHandler<GetAccessibleAccountsQuery, AccessibleAccountsResult>
{
    private readonly ICoinhouseRepository _coinhouses;

    public GetAccessibleAccountsQueryHandler(ICoinhouseRepository coinhouses)
    {
        _coinhouses = coinhouses;
    }

    public async Task<AccessibleAccountsResult> HandleAsync(
        GetAccessibleAccountsQuery query,
        CancellationToken cancellationToken = default)
    {
        CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(query.Coinhouse, cancellationToken);
        if (coinhouse is null)
        {
            return new AccessibleAccountsResult { Accounts = [] };
        }

        if (!Guid.TryParse(query.Persona.Value, out Guid holderGuid))
        {
            return new AccessibleAccountsResult { Accounts = [] };
        }

        // Get the persona's own account (if any)
        Guid ownAccountId = PersonaAccountId.ForCoinhouse(query.Persona, query.Coinhouse);
        CoinhouseAccountDto? ownAccount = await _coinhouses.GetAccountForAsync(ownAccountId, cancellationToken);

        // Get all accounts where the persona is a holder
        IReadOnlyList<CoinhouseAccountDto> sharedAccounts = await _coinhouses
            .GetAccountsForHolderAsync(holderGuid, cancellationToken);

        // Filter to only accounts at this coinhouse
        List<CoinhouseAccountDto> coinhouseAccounts = sharedAccounts
            .Where(a => a.CoinHouseId == coinhouse.Id)
            .ToList();

        // Build the result list
        List<AccessibleAccountInfo> accessibleAccounts = new();
        HashSet<Guid> addedAccountIds = new();

        // Add own account first (if it exists)
        if (ownAccount is not null && ownAccount.CoinHouseId == coinhouse.Id)
        {
            accessibleAccounts.Add(BuildAccountInfo(ownAccount, coinhouse, holderGuid, isOwnAccount: true));
            addedAccountIds.Add(ownAccount.Id);
        }

        // Add shared accounts (excluding own account if already added)
        foreach (CoinhouseAccountDto account in coinhouseAccounts)
        {
            if (addedAccountIds.Contains(account.Id))
            {
                continue;
            }

            accessibleAccounts.Add(BuildAccountInfo(account, coinhouse, holderGuid, isOwnAccount: false));
            addedAccountIds.Add(account.Id);
        }

        return new AccessibleAccountsResult { Accounts = accessibleAccounts };
    }

    private static AccessibleAccountInfo BuildAccountInfo(
        CoinhouseAccountDto account,
        CoinhouseDto coinhouse,
        Guid holderId,
        bool isOwnAccount)
    {
        // Find the holder's role on this account
        CoinhouseAccountHolderDto? holder = account.Holders?.FirstOrDefault(h => h.HolderId == holderId);
        HolderRole role = holder?.Role ?? HolderRole.AuthorizedUser;

        // Build display name
        string displayName = isOwnAccount 
            ? "Personal Account" 
            : BuildSharedAccountName(account, holderId);

        int balance = account.Debit - account.Credit;

        CoinhouseAccountSummary summary = new()
        {
            CoinhouseId = coinhouse.Id,
            CoinhouseTag = new CoinhouseTag(coinhouse.Tag),
            Debit = account.Debit,
            Credit = account.Credit,
            OpenedAt = account.OpenedAt,
            LastAccessedAt = account.LastAccessedAt
        };

        return new AccessibleAccountInfo
        {
            AccountId = account.Id,
            DisplayName = displayName,
            Role = role,
            IsOwnAccount = isOwnAccount,
            Balance = balance,
            Summary = summary,
            Holders = account.Holders ?? []
        };
    }

    private static string BuildSharedAccountName(CoinhouseAccountDto account, Guid currentHolderId)
    {
        // Try to find the primary owner
        CoinhouseAccountHolderDto? owner = account.Holders?
            .FirstOrDefault(h => h.Role == HolderRole.Signatory || h.Role == HolderRole.JointOwner);

        if (owner is not null && owner.HolderId != currentHolderId)
        {
            string ownerName = string.IsNullOrWhiteSpace(owner.LastName)
                ? owner.FirstName
                : $"{owner.FirstName} {owner.LastName}";
            return $"{ownerName}'s Account";
        }

        // Fallback to generic name
        return "Shared Account";
    }
}
