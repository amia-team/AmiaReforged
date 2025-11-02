using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class NpcShopConfiguration : IEntityTypeConfiguration<NpcShopRecord>
{
    public void Configure(EntityTypeBuilder<NpcShopRecord> builder)
    {
        builder.ToTable("NpcShops");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tag)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ShopkeeperTag)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DefinitionHash)
            .HasMaxLength(128);

        builder.HasIndex(x => x.Tag)
            .IsUnique();

        builder.HasMany(x => x.Products)
            .WithOne(x => x.Shop)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.LedgerEntries)
            .WithOne(x => x.Shop)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.VaultItems)
            .WithOne(x => x.Shop)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
