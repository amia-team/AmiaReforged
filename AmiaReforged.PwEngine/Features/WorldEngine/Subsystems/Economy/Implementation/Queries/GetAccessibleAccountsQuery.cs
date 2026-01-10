using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

/// <summary>
/// Retrieves all coinhouse accounts a persona has access to at a specific coinhouse.
/// This includes the persona's own account (if any) plus any accounts where they are a holder.
/// </summary>
public sealed record GetAccessibleAccountsQuery(PersonaId Persona, CoinhouseTag Coinhouse)
    : IQuery<AccessibleAccountsResult>;

/// <summary>
/// Collection of accounts accessible to a persona at a coinhouse.
/// </summary>
public sealed record AccessibleAccountsResult
{
    /// <summary>
    /// All accounts the persona can access at this coinhouse.
    /// </summary>
    public required IReadOnlyList<AccessibleAccountInfo> Accounts { get; init; }
    
    /// <summary>
    /// True if the persona has any accessible accounts.
    /// </summary>
    public bool HasAccounts => Accounts.Count > 0;
    
    /// <summary>
    /// True if the persona has more than one accessible account (enabling selection).
    /// </summary>
    public bool HasMultipleAccounts => Accounts.Count > 1;
}

/// <summary>
/// Information about a single accessible account.
/// </summary>
public sealed record AccessibleAccountInfo
{
    /// <summary>
    /// Unique identifier for the account.
    /// </summary>
    public required Guid AccountId { get; init; }
    
    /// <summary>
    /// Display name for the account (e.g., "Personal Account" or "John Smith's Account").
    /// </summary>
    public required string DisplayName { get; init; }
    
    /// <summary>
    /// The holder's role on this account.
    /// </summary>
    public required HolderRole Role { get; init; }
    
    /// <summary>
    /// True if this is the persona's own account (they are the primary owner).
    /// </summary>
    public required bool IsOwnAccount { get; init; }
    
    /// <summary>
    /// Current balance (debit - credit).
    /// </summary>
    public required int Balance { get; init; }
    
    /// <summary>
    /// The full account summary.
    /// </summary>
    public required CoinhouseAccountSummary Summary { get; init; }
    
    /// <summary>
    /// All holders on this account.
    /// </summary>
    public required IReadOnlyList<CoinhouseAccountHolderDto> Holders { get; init; }
}
