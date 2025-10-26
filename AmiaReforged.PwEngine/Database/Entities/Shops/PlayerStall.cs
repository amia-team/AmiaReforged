using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Shops;

public class PlayerStall
{
    [Key] public long Id { get; set; }

    [MaxLength(255)] public required string Tag { get; set; }
    [MaxLength(16)] public required string AreaResRef { get; set; }
    public Guid? CharacterId { get; set; }
    [ForeignKey(nameof(CharacterId))] public PersistedCharacter? Character { get; set; }

    public List<StallProduct>? Products { get; set; }
}
