using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;

public class PersistentResourceNode
{
    [Key] public long Id { get; set; }

    public long LocationId { get; set; }

    public string ResourceTag { get; set; } = null!;

    [ForeignKey("LocationId")] public SavedLocation Location { get; set; } = null!;
}
