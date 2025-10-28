namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a member's rank within an organization hierarchy
/// </summary>
public enum OrganizationRank
{
    /// <summary>
    /// Applicant or invited member not yet accepted
    /// </summary>
    Pending = 0,

    /// <summary>
    /// New member, lowest rank
    /// </summary>
    Recruit = 1,

    /// <summary>
    /// Standard member
    /// </summary>
    Member = 2,

    /// <summary>
    /// Veteran or senior member
    /// </summary>
    Veteran = 3,

    /// <summary>
    /// Officer or lieutenant
    /// </summary>
    Officer = 4,

    /// <summary>
    /// High officer or commander
    /// </summary>
    Commander = 5,

    /// <summary>
    /// Leader, founder, or guildmaster
    /// </summary>
    Leader = 10
}

