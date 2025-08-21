namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class ReactionContext
{
    // Put any environment flags here as needed (location, station tags, biome, time-of-day, etc.)
    public IReadOnlyDictionary<string, string> Environment { get; }

    public ReactionContext(IReadOnlyDictionary<string, string>? environment = null)
    {
        Environment = environment ?? new Dictionary<string, string>();
    }
}
