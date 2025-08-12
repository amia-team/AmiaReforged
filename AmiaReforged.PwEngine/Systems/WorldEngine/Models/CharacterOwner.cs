namespace AmiaReforged.PwEngine.Systems.WorldEngine.Models;

public abstract record CharacterOwner
{
    private CharacterOwner()
    {
    }

    public sealed record Player(string PublicCdKey) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(PublicCdKey)
            ? throw new ArgumentException("Public CD Key is required.", nameof(PublicCdKey))
            : PublicCdKey;

        public override string ToString() => $"Player:{Key}";
    }

    public sealed record DungeonMaster(string DmKey) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(DmKey)
            ? throw new ArgumentException("DM Key is required.", nameof(DmKey))
            : DmKey;

        public override string ToString() => $"DM:{Key}";
    }

    public sealed record System(string Tag) : CharacterOwner
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(Tag) ? "engine" : Tag;
        public override string ToString() => $"System:{Key}";
    }
}