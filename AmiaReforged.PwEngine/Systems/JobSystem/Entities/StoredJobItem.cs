using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Entities;

public class StoredJobItem
{
    [Key] public long Id { get; set; }

    public long JobItemId { get; set; }
    [ForeignKey(name: "JobItemId")] public JobItem? JobItem { get; set; }

    public long ItemStorageId { get; set; }
    [ForeignKey(name: "ItemStorageId")] public ItemStorage? ItemStorage { get; set; }
}