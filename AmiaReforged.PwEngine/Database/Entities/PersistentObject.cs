using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace AmiaReforged.PwEngine.Database.Entities;

public class PersistentObject
{
    [Key] public long Id { get; set; }
    public required int Type { get; set; }
    public required byte[] Serialized { get; set; }
    public long LocationId { get; set; }
    [ForeignKey("LocationId")] public SavedLocation? Location { get; set; }
}
