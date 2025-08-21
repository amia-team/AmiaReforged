namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class RequiresKnowledge(KnowledgeKey key) : IReactionPrecondition
{
    public KnowledgeKey Key { get; } = key;

    public PreconditionResult Check(ReactionContext context, IReactionActor actor)
        => actor.Knowledge.Contains(Key)
            ? PreconditionResult.Ok()
            : PreconditionResult.Fail("missing_knowledge", $"Requires knowledge '{Key.Value}'.");
}