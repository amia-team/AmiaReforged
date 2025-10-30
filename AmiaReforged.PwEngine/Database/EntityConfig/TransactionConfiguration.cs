using AmiaReforged.PwEngine.Database.Entities.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persona-to-persona transaction ledger mapping.
/// </summary>
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromPersonaId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ToPersonaId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.Memo)
            .HasMaxLength(500);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.HasIndex(x => x.FromPersonaId);
        builder.HasIndex(x => x.ToPersonaId);
        builder.HasIndex(x => x.Timestamp);
    }
}
