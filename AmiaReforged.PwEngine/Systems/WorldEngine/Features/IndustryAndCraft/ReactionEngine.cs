using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

/// <summary>
/// Provides methods to evaluate and execute reactions within the context
/// of the industry's crafting and operational mechanics.
/// </summary>
public sealed class ReactionEngine(
    IReactionDefinitionRepository reactions,
    ICharacterKnowledgeRepository knowledge,
    IToolingPort tools,
    IInventoryPort inventory,
    IRandomPort random)
{
    /// Asynchronously builds an actor representation for a given character, including their knowledge and tools.
    /// <param name="characterId">
    /// The unique identifier of the character for whom the actor is built.
    /// </param>
    /// <param name="ct">
    /// The cancellation token to cancel the asynchronous operation. Default is CancellationToken.None.
    /// </param>
    /// <returns>
    /// An instance of <see cref="IReactionActor"/> representing the actor, containing the character's knowledge and tools.
    /// </returns>
    public async Task<IReactionActor> BuildActorAsync(Guid characterId, CancellationToken ct = default)
    {
        IReadOnlySet<KnowledgeKey> knowledgeLearned = await knowledge.GetSetAsync(characterId, ct);
        IReadOnlyList<ToolInstance> toolInstances = await tools.GetToolsAsync(characterId, ct);
        return new ActorSnapshot(characterId, knowledgeLearned, toolInstances);
    }

    /// <summary>
    /// Evaluates the feasibility of executing a reaction based on the provided reaction definition, context, and actor.
    /// </summary>
    /// <param name="reaction">The definition of the reaction to be evaluated.</param>
    /// <param name="ctx">The context in which the reaction will be executed.</param>
    /// <param name="actor">The actor attempting to execute the reaction.</param>
    /// <returns>A <see cref="ReactionFeasibility"/> object containing the evaluation results, success chance, duration, and other relevant information.</returns>
    public ReactionFeasibility Evaluate(ReactionDefinition reaction, ReactionContext ctx, IReactionActor actor)
    {
        // Preconditions
        PreconditionResult[] results = reaction.Preconditions.Select(p => p.Check(ctx, actor)).ToArray();
        bool canExecute = results.All(r => r.Satisfied);

        // Computation
        Computation comp = new()
        {
            SuccessChance = reaction.BaseSuccessChance,
            Duration = reaction.BaseDuration
        };

        foreach (IReactionModifier mod in reaction.Modifiers)
            mod.Apply(ctx, actor, comp);

        return new ReactionFeasibility(
            canExecute,
            results,
            comp.Duration,
            comp.SuccessChance,
            comp.OutputMultipliers.ToDictionary(kv => kv.Key, kv => kv.Value));
    }

    /// Executes a reaction asynchronously, evaluating its feasibility, consuming inputs, and producing outputs.
    /// <param name="reactionId">The unique identifier of the reaction to execute.</param>
    /// <param name="actorId">The unique identifier of the actor performing the reaction.</param>
    /// <param name="ctx">The context in which the reaction is being executed, containing additional parameters or state.</param>
    /// <param name="ct">A CancellationToken that propagates notification that the operation should be canceled. Defaults to the default token.</param>
    /// <returns>A Task representing the asynchronous operation, returning a ReactionResult that indicates whether the reaction was successful and includes its outputs, duration, and any notes or errors.</returns>
    public async Task<ReactionResult> ExecuteAsync(
        Guid reactionId,
        Guid actorId,
        ReactionContext ctx,
        CancellationToken ct = default)
    {
        ReactionDefinition reaction = await reactions.FindByIdAsync(reactionId, ct)
                                      ?? throw new InvalidOperationException("Reaction not found.");

        IReactionActor actor = await BuildActorAsync(actorId, ct);
        ReactionFeasibility feasibility = Evaluate(reaction, ctx, actor);

        if (!feasibility.CanExecute)
        {
            string[] notes = feasibility.PreconditionResults
                .Where(r => !r.Satisfied)
                .Select(r => r.Message ?? "Precondition failed.")
                .ToArray();
            return new ReactionResult(false, TimeSpan.Zero, [], notes);
        }

        if (!await inventory.HasItemsAsync(actor.ActorId, reaction.Inputs, ct))
            return new ReactionResult(false, TimeSpan.Zero, [],
                ["Missing required inputs."]);

        // Consume inputs
        await inventory.ConsumeAsync(actor.ActorId, reaction.Inputs, ct);

        // Roll success
        double roll = random.NextUnit();
        bool success = roll <= feasibility.SuccessChance;

        // Compute outputs (apply per-item multipliers, flooring to int for quantities)
        Quantity[] outputs = reaction.Outputs
            .Select(o =>
            {
                double mult = feasibility.OutputMultipliers.GetValueOrDefault(o.Item, 1.0);
                int amount = (int)Math.Max(0, Math.Floor(o.Amount * mult));
                return new Quantity(o.Item, amount);
            })
            .ToArray();

        if (success)
            await inventory.ProduceAsync(actor.ActorId, outputs, ct);

        return new ReactionResult(success, feasibility.Duration, success ? outputs : [],
            []);
    }

    /// <summary>
    /// Represents a snapshot of an actor used in reactions within the system,
    /// encapsulating the actor's identifier, knowledge, and tools.
    /// </summary>
    /// <remarks>
    /// This sealed class implements the <see cref="IReactionActor"/> interface and is intended
    /// to provide an immutable representation of the actor's state for reaction processing.
    /// </remarks>
    private sealed class ActorSnapshot(Guid id, IEnumerable<KnowledgeKey> knowledge, IEnumerable<ToolInstance> tools)
        : IReactionActor
    {
        /// <summary>
        /// Gets the unique identifier of the actor.
        /// </summary>
        /// <remarks>
        /// The <see cref="ActorId"/> property represents a globally unique identifier (GUID)
        /// that distinguishes the actor within the context of the reaction engine.
        /// It is essential for identifying and operating on specific actors in systems that rely on
        /// IReactionActor implementations.
        /// </remarks>
        public Guid ActorId { get; } = id;

        /// <summary>
        /// Represents the set of knowledge possessed by an actor participating in a reaction within the industry and craft system.
        /// </summary>
        /// <remarks>
        /// Knowledge is modeled as an immutable set of <see cref="KnowledgeKey"/> instances, enabling the identification and validation
        /// of specific knowledge required for tasks, reactions, or preconditions.
        /// </remarks>
        /// <seealso cref="IReactionActor"/>
        /// <seealso cref="KnowledgeKey"/>
        public ImmutableHashSet<KnowledgeKey> Knowledge { get; } = knowledge.ToImmutableHashSet();

        /// <summary>
        /// Represents a collection of tools possessed by an actor.
        /// Tools are used to fulfill preconditions or modify reactions in the industry and crafting systems.
        /// </summary>
        /// <remarks>
        /// Each tool is represented as an instance of the <see cref="ToolInstance"/> class, which includes metadata
        /// such as tool type and quality. The collection of tools is stored as an immutable array for safety and immutability.
        /// </remarks>
        public ImmutableArray<ToolInstance> Tools { get; } = [..tools];
    }
}
