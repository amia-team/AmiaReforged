using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class PlayerStallLedgerEntryConfiguration : IEntityTypeConfiguration<PlayerStallLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PlayerStallLedgerEntry> builder)
    {
        builder.ToTable("player_stall_ledger_entries");

        builder.Property(l => l.EntryType)
            .HasColumnName("entry_type")
            .IsRequired();

        builder.Property(l => l.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(l => l.Currency)
            .HasColumnName("currency")
            .HasMaxLength(16)
            .HasDefaultValue("gp");

        builder.Property(l => l.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(l => l.StallTransactionId)
            .HasColumnName("stall_transaction_id");

        builder.Property(l => l.OccurredUtc)
            .HasColumnName("occurred_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(l => l.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("text");

        builder.HasIndex(l => new { l.StallId, l.OccurredUtc })
            .HasDatabaseName("player_stall_ledger_entries_idx");
    }
}
