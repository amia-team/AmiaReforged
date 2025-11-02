using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class NpcShopRecord
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

    public int RestockMinMinutes { get; set; }

    public int RestockMaxMinutes { get; set; }

    public DateTime? NextRestockUtc { get; set; }

    public int VaultBalance { get; set; }

    [MaxLength(128)]
    public string? DefinitionHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [InverseProperty(nameof(NpcShopProductRecord.Shop))]
    public List<NpcShopProductRecord> Products { get; set; } = new();

    [InverseProperty(nameof(NpcShopLedgerEntry.Shop))]
    public List<NpcShopLedgerEntry> LedgerEntries { get; set; } = new();

    [InverseProperty(nameof(NpcShopVaultItem.Shop))]
    public List<NpcShopVaultItem> VaultItems { get; set; } = new();
}
