using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class ShopVaultItem
{
    [Key]
    public long Id { get; set; }

    public long ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public ShopRecord? Shop { get; set; }

    public required byte[] ItemData { get; set; }

    [MaxLength(255)]
    public string? ItemName { get; set; }

    [MaxLength(64)]
    public string? ResRef { get; set; }

    public int Quantity { get; set; }

    public DateTime StoredAtUtc { get; set; } = DateTime.UtcNow;
}
