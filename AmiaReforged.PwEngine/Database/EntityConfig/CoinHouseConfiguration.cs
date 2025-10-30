using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Fluent configuration for the CoinHouse aggregate root.
/// </summary>
public class CoinHouseConfiguration : IEntityTypeConfiguration<CoinHouse>
{
    public void Configure(EntityTypeBuilder<CoinHouse> builder)
    {
        builder.ToTable("CoinHouses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tag)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.PersonaIdString)
            .HasMaxLength(200);

        builder.HasIndex(x => x.Tag)
            .IsUnique();

        builder.HasMany(x => x.Accounts)
            .WithOne(x => x.CoinHouse)
            .HasForeignKey(x => x.CoinHouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
