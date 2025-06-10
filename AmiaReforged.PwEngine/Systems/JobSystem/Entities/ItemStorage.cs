using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class ItemStorage
{
    [Key] public long Id { get; set; }

    public virtual ICollection<StoredJobItem> Items { get; set; } = [];
}