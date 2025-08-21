namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;


public readonly record struct ToolTag(string Value)
{
    public static ToolTag From(string value) => new(value.Trim().ToLowerInvariant());
}

public sealed class ToolInstance
{
    public ToolTag Tag { get; }
    public int Quality { get; } // 1..100 or any scale you prefer
    public ToolInstance(ToolTag tag, int quality = 50)
    {
        Tag = tag;
        Quality = quality;
    }
}
