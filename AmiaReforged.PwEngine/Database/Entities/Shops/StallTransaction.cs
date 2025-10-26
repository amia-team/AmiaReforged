using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Shops;

public class StallTransaction
{
    [Key] public long Id { get; set; }
    public string? BuyerName { get; set; }
    public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
    public int PricePaid { get; set; }

    public long StallId { get; set; }
    [ForeignKey(nameof(StallId))] public PlayerStall? Stall { get; set; }

    public Guid? StallOwnerId { get; set; }
    [ForeignKey(nameof(StallOwnerId))] public PersistedCharacter? StallOwner { get; set; }
}
