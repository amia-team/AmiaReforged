namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a character's membership status in an organization
/// </summary>
public enum MembershipStatus
{
    /// <summary>
    /// Application pending approval
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Active member in good standing
    /// </summary>
    Active = 1,

    /// <summary>
    /// Temporarily inactive (leave of absence)
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Left voluntarily
    /// </summary>
    Departed = 3,

    /// <summary>
    /// Kicked/expelled from organization
    /// </summary>
    Expelled = 4,

    /// <summary>
    /// Banned from organization
    /// </summary>
    Banned = 5
}

