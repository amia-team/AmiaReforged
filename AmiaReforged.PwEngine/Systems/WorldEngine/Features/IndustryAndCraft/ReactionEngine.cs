using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public sealed class ReactionEngine(
    IReactionDefinitionRepository reactions,
    ICharacterKnowledgeRepository knowledge,
    IToolingPort tools,
    IInventoryPort inventory,
    IRandomPort random)
{
    public async Task<IReactionActor> BuildActorAsync(Guid characterId, CancellationToken ct = default)
    {
        IReadOnlySet<KnowledgeKey> knowledgeLearned = await knowledge.GetSetAsync(characterId, ct);
        IReadOnlyList<ToolInstance> toolInstances = await tools.GetToolsAsync(characterId, ct);
        return new ActorSnapshot(characterId, knowledgeLearned, toolInstances);
    }

    public ReactionFeasibility Evaluate(ReactionDefinition reaction, ReactionContext ctx, IReactionActor actor)
    {
        // Preconditions
        PreconditionResult[] results = reaction.Preconditions.Select(p => p.Check(ctx, actor)).ToArray();
        bool canExecute = results.All(r => r.Satisfied);

        // Computation
        Computation comp = new Computation
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
            return new ReactionResult(false, TimeSpan.Zero, Array.Empty<Quantity>(), notes);
        }

        if (!await inventory.HasItemsAsync(actor.ActorId, reaction.Inputs, ct))
            return new ReactionResult(false, TimeSpan.Zero, Array.Empty<Quantity>(),
                new[] { "Missing required inputs." });

        // Consume inputs
        await inventory.ConsumeAsync(actor.ActorId, reaction.Inputs, ct);

        // Roll success
        double roll = random.NextUnit();
        bool success = roll <= feasibility.SuccessChance;

        // Compute outputs (apply per-item multipliers, flooring to int for quantities)
        Quantity[] outputs = reaction.Outputs
            .Select(o =>
            {
                double mult = feasibility.OutputMultipliers.TryGetValue(o.Item, out double m) ? m : 1.0;
                int amount = (int)Math.Max(0, Math.Floor(o.Amount * mult));
                return new Quantity(o.Item, amount);
            })
            .ToArray();

        if (success)
            await inventory.ProduceAsync(actor.ActorId, outputs, ct);

        return new ReactionResult(success, feasibility.Duration, success ? outputs : Array.Empty<Quantity>(),
            Array.Empty<string>());
    }

    private sealed class ActorSnapshot : IReactionActor
    {
        public Guid ActorId { get; }
        public System.Collections.Immutable.ImmutableHashSet<KnowledgeKey> Knowledge { get; }
        public System.Collections.Immutable.ImmutableArray<ToolInstance> Tools { get; }

        public ActorSnapshot(Guid id, IEnumerable<KnowledgeKey> knowledge, IEnumerable<ToolInstance> tools)
        {
            ActorId = id;
            Knowledge = knowledge.ToImmutableHashSet();
            Tools = tools.ToImmutableArray();
        }
    }
}
