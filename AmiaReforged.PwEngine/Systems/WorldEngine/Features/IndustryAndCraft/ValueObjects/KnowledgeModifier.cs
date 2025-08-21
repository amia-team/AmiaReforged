namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class KnowledgeModifier : IReactionModifier
{
    public KnowledgeKey Key { get; }
    public double? SuccessChanceDelta { get; init; } // additive
    public double? SuccessChanceMultiplier { get; init; } // multiplicative
    public double? DurationMultiplier { get; init; } // e.g. 0.8 for 20% faster
    public Dictionary<ItemTag, double>? OutputMultipliers { get; init; }

    public KnowledgeModifier(KnowledgeKey key) => Key = key;

    public void Apply(ReactionContext context, IReactionActor actor, Computation c)
    {
        if (!actor.Knowledge.Contains(Key)) return;

        if (SuccessChanceDelta is { } d) c.SuccessChance = Math.Clamp(c.SuccessChance + d, 0, 1);
        if (SuccessChanceMultiplier is { } m) c.SuccessChance = Math.Clamp(c.SuccessChance * m, 0, 1);
        if (DurationMultiplier is { } dm) c.Duration = TimeSpan.FromTicks((long)(c.Duration.Ticks * dm));

        if (OutputMultipliers is { } om)
        {
            foreach ((ItemTag item, double mult) in om)
                c.OutputMultipliers[item] = c.OutputMultipliers.TryGetValue(item, out double cur) ? cur * mult : mult;
        }
    }
}
