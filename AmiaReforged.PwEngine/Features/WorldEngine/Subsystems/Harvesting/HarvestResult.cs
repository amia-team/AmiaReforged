namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

public enum HarvestResult
{
    Finished,
    InProgress,

    /// <summary>
    /// The harvest could not start or continue because the required tool
    /// is not equipped. Produced by tool precondition checks.
    /// </summary>
    NoTool,

    /// <summary>
    /// The harvest failed for a reason other than a missing tool:
    /// unknown node, dispatch failure, depleted node, etc.
    /// Callers needing the reason should dispatch
    /// <c>PerformInteractionCommand</c> directly and read
    /// <c>CommandResult.ErrorMessage</c>.
    /// </summary>
    Failed
}
