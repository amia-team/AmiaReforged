using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures CoinHouseAccount persistence model.
/// </summary>
public class CoinHouseAccountConfiguration : IEntityTypeConfiguration<CoinHouseAccount>
{
    public void Configure(EntityTypeBuilder<CoinHouseAccount> builder)
    {
        builder.ToTable("CoinHouseAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Debit)
            .IsRequired();

        builder.Property(x => x.Credit)
            .IsRequired();

        builder.Property(x => x.OpenedAt)
            .IsRequired();

        builder.Property(x => x.LastAccessedAt);

        builder.HasIndex(x => x.CoinHouseId);

        builder.HasMany(x => x.AccountHolders)
            .WithOne(x => x.Account!)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Receipts)
            .WithOne(x => x.CoinHouseAccount!)
            .HasForeignKey(x => x.CoinHouseAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
