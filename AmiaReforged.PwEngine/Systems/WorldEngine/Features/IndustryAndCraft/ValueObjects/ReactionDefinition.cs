using System.Collections.Immutable;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class ReactionDefinition
{
    public Guid Id { get; }
    public string Name { get; }

    public ImmutableArray<Quantity> Inputs { get; }
    public ImmutableArray<Quantity> Outputs { get; }

    public TimeSpan BaseDuration { get; }
    public double BaseSuccessChance { get; } // 0.0 .. 1.0

    public ImmutableArray<IReactionPrecondition> Preconditions { get; }
    public ImmutableArray<IReactionModifier> Modifiers { get; }

    public ReactionDefinition(
        Guid id,
        string name,
        IEnumerable<Quantity> inputs,
        IEnumerable<Quantity> outputs,
        TimeSpan baseDuration,
        double baseSuccessChance,
        IEnumerable<IReactionPrecondition>? preconditions = null,
        IEnumerable<IReactionModifier>? modifiers = null)
    {
        if (baseSuccessChance is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(baseSuccessChance));
        Id = id;
        Name = name;
        Inputs = inputs?.ToImmutableArray() ?? [];
        Outputs = outputs?.ToImmutableArray() ?? [];
        BaseDuration = baseDuration;
        BaseSuccessChance = baseSuccessChance;
        Preconditions = [..preconditions ?? []];
        Modifiers = [..modifiers ?? []];
    }
}
