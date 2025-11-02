using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class ShopRecord
{
    [Key]
    public long Id { get; set; }

    [MaxLength(128)]
    public required string Tag { get; set; }

    [MaxLength(255)]
    public required string DisplayName { get; set; }

    [MaxLength(255)]
    public required string ShopkeeperTag { get; set; }

    public string? Description { get; set; }

    public ShopKind Kind { get; set; } = ShopKind.Npc;

    public bool ManualRestock { get; set; }

    public bool ManualPricing { get; set; }

    public Guid? OwnerAccountId { get; set; }

    [ForeignKey(nameof(OwnerAccountId))]
    public CoinHouseAccount? OwnerAccount { get; set; }

    public Guid? OwnerCharacterId { get; set; }

    [MaxLength(255)]
    public string? OwnerDisplayName { get; set; }

    public int RestockMinMinutes { get; set; }

    public int RestockMaxMinutes { get; set; }

    public DateTime? NextRestockUtc { get; set; }

    public int VaultBalance { get; set; }

    [MaxLength(128)]
    public string? DefinitionHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [InverseProperty(nameof(ShopProductRecord.Shop))]
    public List<ShopProductRecord> Products { get; set; } = new();

    [InverseProperty(nameof(ShopLedgerEntry.Shop))]
    public List<ShopLedgerEntry> LedgerEntries { get; set; } = new();

    [InverseProperty(nameof(ShopVaultItem.Shop))]
    public List<ShopVaultItem> VaultItems { get; set; } = new();
}
