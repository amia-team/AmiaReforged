namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class RequiresTool : IReactionPrecondition
{
    public ToolTag Tool { get; }
    public RequiresTool(ToolTag tool) => Tool = tool;

    public PreconditionResult Check(ReactionContext context, IReactionActor actor)
        => actor.Tools.Any(t => t.Tag == Tool)
            ? PreconditionResult.Ok()
            : PreconditionResult.Fail("missing_tool", $"Requires tool '{Tool.Value}'.");
}