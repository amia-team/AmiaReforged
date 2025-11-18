using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class PlayerStallLedgerEntry
{
    [Key]
    public long Id { get; set; }

    public long StallId { get; set; }

    [ForeignKey(nameof(StallId))]
    public PlayerStall? Stall { get; set; }

    /// <summary>
    /// The owner character ID at the time this ledger entry was created.
    /// Used to filter ledger entries by owner tenure.
    /// </summary>
    public Guid? OwnerCharacterId { get; set; }

    /// <summary>
    /// The owner persona ID at the time this ledger entry was created.
    /// Provides additional context for persona-level tracking.
    /// </summary>
    [MaxLength(256)]
    public string? OwnerPersonaId { get; set; }

    public PlayerStallLedgerEntryType EntryType { get; set; }

    /// <summary>
    /// Amount applied to the ledger entry in gold pieces.
    /// Positive amounts credit the stall; negative amounts debit it.
    /// </summary>
    public int Amount { get; set; }

    [MaxLength(16)]
    public string Currency { get; set; } = "gp";

    [MaxLength(512)]
    public string? Description { get; set; }

    public long? StallTransactionId { get; set; }

    [ForeignKey(nameof(StallTransactionId))]
    public StallTransaction? Transaction { get; set; }

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;

    public string? MetadataJson { get; set; }
}

public enum PlayerStallLedgerEntryType
{
    Unknown = 0,
    RentCharge = 1,
    RentPayment = 2,
    SaleGross = 3,
    SaleNet = 4,
    Withdrawal = 5,
    Deposit = 6,
    Adjustment = 7
}
