using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Shops;

public class StallProduct
{
    [Key] public long Id { get; set; }

    /// <summary>
    /// The exact name of the item as seen in game.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Should include item properties, not only the description text.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The price as set by the player in their stall.
    /// </summary>
    public required int Price { get; set; }

    public required byte[] ItemData { get; set; }

    public long? ShopId { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey(nameof(ShopId))] public PlayerStall? Shop { get; set; }
}
