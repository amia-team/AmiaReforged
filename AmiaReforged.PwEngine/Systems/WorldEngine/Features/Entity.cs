namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features;

public abstract class Entity
{
    public Guid Id { get; init; }
    public DateTime LastUpdated { get; set; }
}
