namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

public class CharacterTrait
{
    public Guid Id { get; init; }
    public required Guid CharacterId { get; init; }
    public required string TraitTag { get; init; }
    public DateTime DateAcquired { get; init; }
    public bool IsConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsUnlocked { get; init; }
    public string? CustomData { get; set; }
}
