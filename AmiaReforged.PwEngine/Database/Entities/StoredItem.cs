using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities;

public class StoredItem
{
    [Key] public long Id { get; set; }

    public required byte[] ItemData { get; set; }

    public required Guid Owner { get; set; }

    public long? WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))] public Storage? Warehouse { get; set; }
}
