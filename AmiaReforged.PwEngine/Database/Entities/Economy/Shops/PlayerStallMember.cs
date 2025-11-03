using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class PlayerStallMember
{
    [Key]
    public long Id { get; set; }

    public long StallId { get; set; }

    [ForeignKey(nameof(StallId))]
    public PlayerStall? Stall { get; set; }

    [MaxLength(256)]
    public required string PersonaId { get; set; }

    [MaxLength(255)]
    public required string DisplayName { get; set; }

    public bool CanManageInventory { get; set; }

    public bool CanConfigureSettings { get; set; }

    public bool CanCollectEarnings { get; set; }

    [MaxLength(256)]
    public string? AddedByPersonaId { get; set; }

    public DateTime AddedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedUtc { get; set; }
}
