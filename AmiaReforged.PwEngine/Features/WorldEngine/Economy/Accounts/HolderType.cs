using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;

/// <summary>
/// Enumerates the types of personas that can hold a coinhouse account.
/// </summary>
public enum HolderType
{
    Individual = 0,
    Organization = 1,
    Government = 2
}

/// <summary>
/// Enumerates the roles an account holder can occupy.
/// </summary>
public enum HolderRole
{
    Owner = 0,
    Signatory = 1,
    Viewer = 2,
    JointOwner = 3,
    AuthorizedUser = 4,
    Trustee = 5
}
