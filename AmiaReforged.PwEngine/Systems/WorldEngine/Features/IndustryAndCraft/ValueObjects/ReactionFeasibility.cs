namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

/// <summary>
/// Represents the feasibility of executing a reaction within a given context.
/// </summary>
public sealed class ReactionFeasibility
{
    /// <summary>
    /// Indicates whether the reaction can be executed based on the evaluation of all preconditions and requirements.
    /// </summary>
    /// <remarks>
    /// The value is determined by assessing the feasibility of the reaction, including the availability
    /// of required resources, satisfaction of preconditions, and other constraints.
    /// </remarks>
    public bool CanExecute { get; }

    /// <summary>
    /// A collection of <see cref="PreconditionResult"/> objects that represent the outcomes
    /// of evaluated preconditions for executing a reaction.
    /// </summary>
    /// <remarks>
    /// Each precondition result indicates whether a specific precondition was satisfied,
    /// along with optional details such as reason codes and messages explaining failure.
    /// This property is used to provide insights into why a reaction can or cannot be executed.
    /// </remarks>
    public IReadOnlyList<PreconditionResult> PreconditionResults { get; }

    /// <summary>
    /// Represents the duration required to complete the reaction.
    /// </summary>
    /// <remarks>
    /// The duration indicates the time span necessary for processing a specific reaction in the system.
    /// It is primarily used to manage timing in reaction execution logic, affecting the overall flow of the industry and crafting processes.
    /// </remarks>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the success probability of a reaction execution, represented as a value between 0.0 and 1.0.
    /// </summary>
    /// <remarks>
    /// A higher value indicates a greater likelihood of the reaction succeeding when executed.
    /// </remarks>
    public double SuccessChance { get; }

    /// <summary>
    /// Represents a collection of output multipliers for specific items, used to adjust the quantities
    /// of items produced during a reaction process. The multipliers are stored as key-value pairs,
    /// where the key is an <see cref="ItemTag"/> identifying the item, and the value is a <see cref="double"/>
    /// representing the multiplier to apply to the output quantity of that item.
    /// </summary>
    /// <remarks>
    /// A multiplier of 1.0 indicates no modification to the output quantity of the respective item.
    /// Values greater than 1.0 increase the output amount, while values less than 1.0 decrease it.
    /// This property is typically used in conjunction with reaction feasibility calculations to factor
    /// in modifiers such as knowledge level, tools, or other influences on the crafting or production process.
    /// </remarks>
    public IReadOnlyDictionary<ItemTag, double> OutputMultipliers { get; }

    /// <summary>
    /// Represents the feasibility of executing a specific reaction within the system,
    /// including whether it can be executed, the reasons for feasibility (or lack thereof),
    /// the duration of the reaction, the success chance, and any output multipliers.
    /// </summary>
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
