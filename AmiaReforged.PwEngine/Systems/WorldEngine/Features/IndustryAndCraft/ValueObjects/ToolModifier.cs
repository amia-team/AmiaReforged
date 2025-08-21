namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class ToolModifier : IReactionModifier
{
    public ToolTag Tool { get; }
    public double? SuccessChanceDelta { get; init; }
    public double? DurationMultiplier { get; init; }
    public Dictionary<ItemTag, double>? OutputMultipliers { get; init; }

    public ToolModifier(ToolTag tool) => Tool = tool;

    public void Apply(ReactionContext context, IReactionActor actor, Computation c)
    {
        ToolInstance? tool = actor.Tools.FirstOrDefault(t => t.Tag == Tool);
        if (tool is null) return;

        double qualityFactor = 1.0 + Math.Clamp((tool.Quality - 50) / 100.0, -0.4, 0.5); // example scaling
        if (SuccessChanceDelta is { } d) c.SuccessChance = Math.Clamp(c.SuccessChance + d * qualityFactor, 0, 1);
        if (DurationMultiplier is { } dm) c.Duration = TimeSpan.FromTicks((long)(c.Duration.Ticks * (dm / qualityFactor)));

        if (OutputMultipliers is { } om)
        {
            foreach ((ItemTag item, double mult) in om)
                c.OutputMultipliers[item] = c.OutputMultipliers.TryGetValue(item, out double cur) ? cur * mult : mult;
        }
    }
}
