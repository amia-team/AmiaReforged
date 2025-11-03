using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class StallTransaction
{
    [Key]
    public long Id { get; set; }

    public long StallId { get; set; }

    [ForeignKey(nameof(StallId))]
    public PlayerStall? Stall { get; set; }

    public long? StallProductId { get; set; }

    [ForeignKey(nameof(StallProductId))]
    public StallProduct? Product { get; set; }

    [MaxLength(256)]
    public string? BuyerPersonaId { get; set; }

    [MaxLength(255)]
    public string? BuyerDisplayName { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Total gold paid by the buyer for this transaction (quantity * unit price).
    /// </summary>
    public int GrossAmount { get; set; }

    /// <summary>
    /// Gold deposited directly to a coinhouse account as part of settlement.
    /// </summary>
    public int DepositAmount { get; set; }

    /// <summary>
    /// Gold retained in the stall's escrow balance for later withdrawal.
    /// </summary>
    public int EscrowAmount { get; set; }

    /// <summary>
    /// Fees or rent offsets withheld at the time of sale.
    /// </summary>
    public int FeeAmount { get; set; }

    public long? CoinHouseTransactionId { get; set; }

    [ForeignKey(nameof(CoinHouseTransactionId))]
    public CoinHouseTransaction? CoinHouseTransaction { get; set; }

    [MaxLength(512)]
    public string? Notes { get; set; }
}
