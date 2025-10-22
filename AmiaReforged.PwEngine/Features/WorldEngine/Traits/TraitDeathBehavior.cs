namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Defines how a trait responds to character death.
/// </summary>
public enum TraitDeathBehavior
{
    /// <summary>
    /// Trait persists through death unchanged (most traits).
    /// </summary>
    Persist = 0,

    /// <summary>
    /// Trait becomes inactive on death, requires rebuilding (Hero trait).
    /// CustomData is cleared on death.
    /// </summary>
    ResetOnDeath = 1,

    /// <summary>
    /// Character permadeath if killed by specific conditions (Villain trait).
    /// </summary>
    Permadeath = 2,

    /// <summary>
    /// Trait is removed entirely on death.
    /// </summary>
    RemoveOnDeath = 3
}
