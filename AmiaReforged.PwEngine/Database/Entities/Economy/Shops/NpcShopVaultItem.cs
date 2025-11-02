using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class NpcShopVaultItem
{
    [Key]
    public long Id { get; set; }

    public long ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public NpcShopRecord? Shop { get; set; }

    [Required]
    public required byte[] ItemData { get; set; }

    public DateTime StoredAt { get; set; } = DateTime.UtcNow;

    public DateTime? RetrievedAt { get; set; }

    public string? MetadataJson { get; set; }
}
