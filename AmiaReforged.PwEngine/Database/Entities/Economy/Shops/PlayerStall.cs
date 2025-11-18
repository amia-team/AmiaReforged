using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

public class PlayerStall
{
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Unique identifier for the stall placeable within an area.
    /// </summary>
    [MaxLength(255)]
    public required string Tag { get; set; }

    /// <summary>
    /// NWN area resref where the stall exists; used to enforce one-stall-per-area ownership.
    /// </summary>
    [MaxLength(64)]
    public required string AreaResRef { get; set; }

    /// <summary>
    /// Optional settlement identifier for reporting and account routing.
    /// </summary>
    [MaxLength(128)]
    public string? SettlementTag { get; set; }

    /// <summary>
    /// Primary owner persona and display data.
    /// </summary>
    public Guid? OwnerCharacterId { get; set; }

    [MaxLength(256)]
    public string? OwnerPersonaId { get; set; }

    [MaxLength(256)]
    public string? OwnerPlayerPersonaId { get; set; }

    [MaxLength(255)]
    public string? OwnerDisplayName { get; set; }

    /// <summary>
    /// Account used for rent withdrawal and automatic deposit of proceeds when enabled.
    /// </summary>
    public Guid? CoinHouseAccountId { get; set; }

    [ForeignKey(nameof(CoinHouseAccountId))]
    public CoinHouseAccount? CoinHouseAccount { get; set; }

    /// <summary>
    /// When true, profits remain in the stall escrow instead of auto-depositing to the account.
    /// </summary>
    public bool HoldEarningsInStall { get; set; }

    /// <summary>
    /// Gold currently held by the stall awaiting withdrawal by the owner.
    /// </summary>
    public int EscrowBalance { get; set; }

    /// <summary>
    /// Gross sales for the CURRENT owner's tenure only.
    /// Resets when ownership changes.
    /// </summary>
    public int CurrentTenureGrossSales { get; set; }

    /// <summary>
    /// Net earnings for the CURRENT owner's tenure only.
    /// Resets when ownership changes.
    /// </summary>
    public int CurrentTenureNetEarnings { get; set; }

    /// <summary>
    /// Lifetime gross sales attributed to this stall across all owners.
    /// For historical/administrative tracking only.
    /// </summary>
    public int LifetimeGrossSales { get; set; }

    /// <summary>
    /// Lifetime net earnings after rent and fees across all owners.
    /// For historical/administrative tracking only.
    /// </summary>
    public int LifetimeNetEarnings { get; set; }

    /// <summary>
    /// Daily rent charged for maintaining the stall.
    /// </summary>
    public int DailyRent { get; set; } = 10_000;

    public DateTime LeaseStartUtc { get; set; } = DateTime.UtcNow;

    public DateTime NextRentDueUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastRentPaidUtc { get; set; }

    /// <summary>
    /// When populated, the stall is not available for transactions.
    /// </summary>
    public DateTime? SuspendedUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeactivatedUtc { get; set; }

    public List<StallProduct> Inventory { get; set; } = new();

    public List<PlayerStallMember> Members { get; set; } = new();

    public List<PlayerStallLedgerEntry> LedgerEntries { get; set; } = new();

    public List<StallTransaction> Transactions { get; set; } = new();
}
