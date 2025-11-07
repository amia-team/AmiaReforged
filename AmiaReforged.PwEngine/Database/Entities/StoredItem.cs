using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities;

public class StoredItem
{
    [Key] public long Id { get; set; }

    public required byte[] ItemData { get; set; }

    public required Guid Owner { get; set; }

    /// <summary>
    /// Display name of the stored item (for UI purposes)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional description of the stored item
    /// </summary>
    public string? Description { get; set; }

    public long? WarehouseId { get; set; }
    [ForeignKey(nameof(WarehouseId))] public Storage? Warehouse { get; set; }
}
