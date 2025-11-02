using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class ShopProductRecord
{
    [Key]
    public long Id { get; set; }

    public long ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public ShopRecord? Shop { get; set; }

    [MaxLength(64)]
    public required string ResRef { get; set; }

    public int Price { get; set; }

    public int CurrentStock { get; set; }

    public int MaxStock { get; set; }

    public int RestockAmount { get; set; }

    public bool IsPlayerManaged { get; set; }

    public int SortOrder { get; set; }

    public string? LocalVariablesJson { get; set; }

    public string? AppearanceJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
