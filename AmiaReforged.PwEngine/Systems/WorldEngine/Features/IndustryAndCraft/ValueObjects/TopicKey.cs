namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public readonly record struct TopicKey(string Value)
{
    public static TopicKey From(string value) => new(value.Trim().ToLowerInvariant());
}
