using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.PlayerHousing;

public class House
{
    [Key] public long Id { get; set; }
    public required string Tag { get; set; }
    public required int Settlement { get; set; }
    public Guid? CharacterId { get; set; }
    [ForeignKey("CharacterId")] public PersistedCharacter? Character { get; set; }

}
