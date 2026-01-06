using AmiaReforged.PwEngine.Database.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for dreamcoin rental records.
/// </summary>
public sealed class DreamcoinRentalConfiguration : IEntityTypeConfiguration<DreamcoinRental>
{
    public void Configure(EntityTypeBuilder<DreamcoinRental> builder)
    {
        builder.ToTable("dreamcoin_rentals");

        builder.HasKey(r => r.Id)
            .HasName("dreamcoin_rentals_pkey");

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.PlayerCdKey)
            .HasColumnName("player_cd_key")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.MonthlyCost)
            .HasColumnName("monthly_cost")
            .IsRequired();

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(r => r.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.CreatedByDmCdKey)
            .HasColumnName("created_by_dm_cd_key")
            .HasMaxLength(64);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.IsDelinquent)
            .HasColumnName("is_delinquent")
            .HasDefaultValue(false);

        builder.Property(r => r.LastPaymentUtc)
            .HasColumnName("last_payment_utc");

        builder.Property(r => r.NextDueDateUtc)
            .HasColumnName("next_due_date_utc");

        builder.HasIndex(r => r.PlayerCdKey)
            .HasDatabaseName("dreamcoin_rentals_player_cd_key_idx");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("dreamcoin_rentals_is_active_idx");

        builder.HasIndex(r => r.IsDelinquent)
            .HasDatabaseName("dreamcoin_rentals_is_delinquent_idx");

        builder.HasIndex(r => r.NextDueDateUtc)
            .HasDatabaseName("dreamcoin_rentals_next_due_date_idx");
    }
}
