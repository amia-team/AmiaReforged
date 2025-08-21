namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public interface IReactionPrecondition
{
    PreconditionResult Check(ReactionContext context, IReactionActor actor);
}