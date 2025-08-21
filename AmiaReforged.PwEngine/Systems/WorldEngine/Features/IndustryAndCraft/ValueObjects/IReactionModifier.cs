namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public interface IReactionModifier
{
    void Apply(ReactionContext context, IReactionActor actor, Computation computation);
}
