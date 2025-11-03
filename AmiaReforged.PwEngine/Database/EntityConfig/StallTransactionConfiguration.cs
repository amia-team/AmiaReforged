using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class StallTransactionConfiguration : IEntityTypeConfiguration<StallTransaction>
{
    public void Configure(EntityTypeBuilder<StallTransaction> builder)
    {
        builder.ToTable("player_stall_transactions");

        builder.Property(t => t.StallId)
            .HasColumnName("stall_id")
            .IsRequired();

        builder.Property(t => t.StallProductId)
            .HasColumnName("stall_product_id");

        builder.Property(t => t.BuyerPersonaId)
            .HasColumnName("buyer_persona_id")
            .HasMaxLength(256);

        builder.Property(t => t.BuyerDisplayName)
            .HasColumnName("buyer_display_name")
            .HasMaxLength(255);

        builder.Property(t => t.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1);

        builder.Property(t => t.GrossAmount)
            .HasColumnName("gross_amount")
            .IsRequired();

        builder.Property(t => t.DepositAmount)
            .HasColumnName("deposit_amount");

        builder.Property(t => t.EscrowAmount)
            .HasColumnName("escrow_amount");

        builder.Property(t => t.FeeAmount)
            .HasColumnName("fee_amount");

        builder.Property(t => t.CoinHouseTransactionId)
            .HasColumnName("coinhouse_transaction_id");

        builder.Property(t => t.Notes)
            .HasColumnName("notes")
            .HasMaxLength(512);

        builder.HasIndex(t => new { t.StallId, t.OccurredAtUtc })
            .HasDatabaseName("player_stall_transactions_idx");
    }
}
