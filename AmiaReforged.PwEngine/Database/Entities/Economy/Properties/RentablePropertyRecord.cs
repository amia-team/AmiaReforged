using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

namespace AmiaReforged.PwEngine.Database.Entities.Economy.Properties;

/// <summary>
/// EF representation of a rentable property definition and its runtime state.
/// </summary>
public class RentablePropertyRecord
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(128)]
    public required string InternalName { get; set; }

    [MaxLength(128)]
    public required string SettlementTag { get; set; }

    public PropertyCategory Category { get; set; }

    public int MonthlyRent { get; set; }

    public bool AllowsCoinhouseRental { get; set; }

    public bool AllowsDirectRental { get; set; }

    [MaxLength(128)]
    public string? SettlementCoinhouseTag { get; set; }

    public int? PurchasePrice { get; set; }

    public int? MonthlyOwnershipTax { get; set; }

    public int EvictionGraceDays { get; set; }

    [MaxLength(256)]
    public string? DefaultOwnerPersona { get; set; }

    public PropertyOccupancyStatus OccupancyStatus { get; set; }

    [MaxLength(256)]
    public string? CurrentTenantPersona { get; set; }

    [MaxLength(256)]
    public string? CurrentOwnerPersona { get; set; }

    public DateOnly? RentalStartDate { get; set; }

    public DateOnly? NextPaymentDueDate { get; set; }

    public int? RentalMonthlyRent { get; set; }

    public RentalPaymentMethod? RentalPaymentMethod { get; set; }

    public DateTimeOffset? LastOccupantSeenUtc { get; set; }

    public List<RentablePropertyResidentRecord> Residents { get; set; } = new();
}
