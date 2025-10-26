using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

public class Storage
{
    [Key] public long Id { get; set; }

    public required Guid EngineId { get; set; }

    public Guid? OwnerId { get; set; }

    public List<StoredItem>? Items { get; set; }

    /// <summary>
    ///  The maximum number of items this storage can hold. -1 is unlimited.
    /// </summary>
    public int Capacity { get; set; } = -1;
}
