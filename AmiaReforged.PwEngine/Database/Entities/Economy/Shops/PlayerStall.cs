using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class PlayerStall
{
    [Key] public long Id { get; set; }

    [MaxLength(255)] public required string Tag { get; set; }
    [MaxLength(16)] public required string AreaResRef { get; set; }
    public Guid? AccountId { get; set; }
    [ForeignKey(nameof(AccountId))] public CoinHouseAccount? Account { get; set; }

    public int StoredGold { get; set; }
    public int GrossProfit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastPaidRentAt { get; set; }


    public List<StallProduct>? Products { get; set; }
    public List<StallTransaction>? Transactions { get; set; }
}
