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

    /// <summary>
    /// The type of storage: PlayerInventory, ForeclosedItems, CoinhouseVault, etc.
    /// </summary>
    [MaxLength(50)]
    public required string StorageType { get; set; } = "PlayerInventory";

    /// <summary>
    /// Location key for storage types that are tied to a specific location.
    /// Format: "{type}:{identifier}" (e.g., "coinhouse:cordor_coinhouse")
    /// </summary>
    [MaxLength(255)]
    public string? LocationKey { get; set; }
}
