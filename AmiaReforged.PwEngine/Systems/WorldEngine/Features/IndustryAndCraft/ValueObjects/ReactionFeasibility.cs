namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class ReactionFeasibility
{
    public bool CanExecute { get; }
    public IReadOnlyList<PreconditionResult> PreconditionResults { get; }
    public TimeSpan Duration { get; }
    public double SuccessChance { get; }
    public IReadOnlyDictionary<ItemTag, double> OutputMultipliers { get; }

    public ReactionFeasibility(
        bool canExecute,
        IReadOnlyList<PreconditionResult> preconditionResults,
        TimeSpan duration,
        double successChance,
        IReadOnlyDictionary<ItemTag, double> outputMultipliers)
    {
        CanExecute = canExecute;
        PreconditionResults = preconditionResults;
        Duration = duration;
        SuccessChance = successChance;
        OutputMultipliers = outputMultipliers;
    }
}
