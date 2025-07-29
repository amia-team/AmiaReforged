using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy;

public class CharacterFieldExperience
{
    [Key] public required long Id { get; set; }
    public int Level { get; set; }
    public float Experience { get; set; }

    public required long CharacterId { get; set; }
    [ForeignKey("CharacterId")] public required PersistedWorldCharacter Character { get; set; }
    
    
}