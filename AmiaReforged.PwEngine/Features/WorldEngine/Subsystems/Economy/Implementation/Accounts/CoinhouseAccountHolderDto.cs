namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

/// <summary>
/// Lightweight projection of an account holder record for coinhouse accounts.
/// </summary>
public sealed record CoinhouseAccountHolderDto
{
    /// <summary>
    /// Database identifier for the holder entry, if persisted.
    /// </summary>
    public long? Id { get; init; }

    /// <summary>
    /// Identifier of the persona or organization granted access to the account.
    /// </summary>
    public required Guid HolderId { get; init; }

    /// <summary>
    /// Describes the type of holder (individual, organization, etc.).
    /// </summary>
    public required HolderType Type { get; init; }

    /// <summary>
    /// Describes the holder's permission level within the account.
    /// </summary>
    public required HolderRole Role { get; init; }

    /// <summary>
    /// Convenience display field for the holder's first name or leading label.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Convenience display field for the holder's last name or trailing label.
    /// </summary>
    public required string LastName { get; init; }
}
