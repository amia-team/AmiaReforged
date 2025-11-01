using AmiaReforged.PwEngine.Database.Entities.Economy.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class RentablePropertyConfiguration : IEntityTypeConfiguration<RentablePropertyRecord>
{
    public void Configure(EntityTypeBuilder<RentablePropertyRecord> builder)
    {
        builder.ToTable("rentable_properties");

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.InternalName)
            .HasColumnName("internal_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.SettlementTag)
            .HasColumnName("settlement_tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.Category)
            .HasColumnName("category")
            .HasConversion<int>();

        builder.Property(p => p.MonthlyRent)
            .HasColumnName("monthly_rent");

        builder.Property(p => p.AllowsCoinhouseRental)
            .HasColumnName("allows_coinhouse_rental");

        builder.Property(p => p.AllowsDirectRental)
            .HasColumnName("allows_direct_rental");

        builder.Property(p => p.SettlementCoinhouseTag)
            .HasColumnName("settlement_coinhouse_tag")
            .HasMaxLength(128);

        builder.Property(p => p.PurchasePrice)
            .HasColumnName("purchase_price");

        builder.Property(p => p.MonthlyOwnershipTax)
            .HasColumnName("monthly_ownership_tax");

        builder.Property(p => p.EvictionGraceDays)
            .HasColumnName("eviction_grace_days");

        builder.Property(p => p.DefaultOwnerPersona)
            .HasColumnName("default_owner_persona")
            .HasMaxLength(256);

        builder.Property(p => p.OccupancyStatus)
            .HasColumnName("occupancy_status")
            .HasConversion<int>();

        builder.Property(p => p.CurrentTenantPersona)
            .HasColumnName("current_tenant_persona")
            .HasMaxLength(256);

        builder.Property(p => p.CurrentOwnerPersona)
            .HasColumnName("current_owner_persona")
            .HasMaxLength(256);

        builder.Property(p => p.RentalStartDate)
            .HasColumnName("rental_start_date");

        builder.Property(p => p.NextPaymentDueDate)
            .HasColumnName("next_payment_due_date");

        builder.Property(p => p.RentalMonthlyRent)
            .HasColumnName("rental_monthly_rent");

        builder.Property(p => p.RentalPaymentMethod)
            .HasColumnName("rental_payment_method")
            .HasConversion<int?>();

        builder.Property(p => p.LastOccupantSeenUtc)
            .HasColumnName("last_occupant_seen_utc");

        builder.HasIndex(p => p.InternalName)
            .HasDatabaseName("rentable_properties_internal_name_idx");

        builder.HasIndex(p => p.SettlementTag)
            .HasDatabaseName("rentable_properties_settlement_idx");

        builder.HasMany(p => p.Residents)
            .WithOne(r => r.Property)
            .HasForeignKey(r => r.PropertyId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Residents)
            .AutoInclude();
    }
}
