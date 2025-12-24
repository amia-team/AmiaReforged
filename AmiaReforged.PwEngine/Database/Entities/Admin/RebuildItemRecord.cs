using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Admin;

public class RebuildItemRecord
{
    [Key] public long Id { get; set; }
    public int CharacterRebuildId { get; set; }

    [ForeignKey(nameof(CharacterRebuildId))]
    public CharacterRebuild? CharacterRebuild { get; set; }

    public required byte[] ItemData { get; set; }
}
