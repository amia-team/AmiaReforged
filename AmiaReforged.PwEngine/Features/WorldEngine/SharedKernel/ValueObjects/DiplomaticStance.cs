namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents the diplomatic relationship between two organizations
/// </summary>
public enum DiplomaticStance
{
    /// <summary>
    /// No relationship established
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Neutral relationship, no special bonds
    /// </summary>
    Neutral = 1,

    /// <summary>
    /// Friendly relationship, possible cooperation
    /// </summary>
    Friendly = 2,

    /// <summary>
    /// Formal alliance, mutual support
    /// </summary>
    Allied = 3,

    /// <summary>
    /// Unfriendly relationship, tension
    /// </summary>
    Unfriendly = -1,

    /// <summary>
    /// Active rivalry, competition
    /// </summary>
    Rival = -2,

    /// <summary>
    /// Hostile relationship, open conflict
    /// </summary>
    Hostile = -3,

    /// <summary>
    /// Declared war
    /// </summary>
    War = -10
}

