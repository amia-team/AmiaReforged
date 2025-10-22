using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistentCharacterTrait
{
    [Key] public required Guid Id { get; init; }
    
    public required Guid CharacterId { get; init; }
    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; init; }
    
    public required string TraitTag { get; init; }
    
    public required DateTime DateAcquired { get; init; }
    
    public required bool IsConfirmed { get; set; }
    
    public required bool IsActive { get; set; }
    
    public required bool IsUnlocked { get; init; }
    
    public string? CustomData { get; set; }
}
