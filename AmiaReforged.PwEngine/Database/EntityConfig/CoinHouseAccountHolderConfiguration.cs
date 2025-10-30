using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures CoinHouseAccountHolder persistence model.
/// </summary>
public class CoinHouseAccountHolderConfiguration : IEntityTypeConfiguration<CoinHouseAccountHolder>
{
    public void Configure(EntityTypeBuilder<CoinHouseAccountHolder> builder)
    {
        builder.ToTable("CoinHouseAccountHolders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.HolderId)
            .IsRequired();

        builder.HasIndex(x => new { x.HolderId, x.AccountId })
            .IsUnique();
    }
}
