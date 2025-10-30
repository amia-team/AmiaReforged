using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures CoinHouseTransaction persistence model.
/// </summary>
public class CoinHouseTransactionConfiguration : IEntityTypeConfiguration<CoinHouseTransaction>
{
    public void Configure(EntityTypeBuilder<CoinHouseTransaction> builder)
    {
        builder.ToTable("CoinHouseTransactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .IsRequired();

        builder.Property(x => x.IssuerType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.IssuedAt)
            .IsRequired();

        builder.Property(x => x.ProcessedAt);

        builder.HasIndex(x => x.CoinHouseAccountId);
    }
}
