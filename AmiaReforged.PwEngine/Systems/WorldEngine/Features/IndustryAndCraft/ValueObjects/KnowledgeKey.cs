namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public readonly record struct KnowledgeKey(string Value)
{
    public static KnowledgeKey From(string value) =>
        new(value.Trim().ToLowerInvariant());
}
