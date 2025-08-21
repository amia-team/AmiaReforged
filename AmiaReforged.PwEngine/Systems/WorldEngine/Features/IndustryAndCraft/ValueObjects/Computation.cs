namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class Computation // mutable working copy during evaluation
{
    public double SuccessChance; // 0..1
    public TimeSpan Duration;
    public Dictionary<ItemTag, double> OutputMultipliers { get; } = new();
}