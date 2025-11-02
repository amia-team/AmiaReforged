using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class ShopLedgerEntry
{
    [Key]
    public long Id { get; set; }

    public long ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public ShopRecord? Shop { get; set; }

    [MaxLength(255)]
    public string? BuyerName { get; set; }

    [MaxLength(128)]
    public string? BuyerPersona { get; set; }

    public int Quantity { get; set; }

    public int UnitPrice { get; set; }

    public int TotalPrice { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(64)]
    public string? ResRef { get; set; }

    public string? Notes { get; set; }
}
