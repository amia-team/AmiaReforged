using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class NpcShopLedgerEntry
{
    [Key]
    public long Id { get; set; }

    public long ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public NpcShopRecord? Shop { get; set; }

    [MaxLength(64)]
    public required string ProductResRef { get; set; }

    [MaxLength(255)]
    public string? ProductName { get; set; }

    public int Quantity { get; set; }

    public int SalePrice { get; set; }

    [MaxLength(255)]
    public string? BuyerName { get; set; }

    public DateTime SoldAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
