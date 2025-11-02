using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class NpcShopVaultItemConfiguration : IEntityTypeConfiguration<NpcShopVaultItem>
{
    public void Configure(EntityTypeBuilder<NpcShopVaultItem> builder)
    {
        builder.ToTable("NpcShopVaultItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ItemData)
            .IsRequired();

        builder.Property(x => x.MetadataJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ShopId, x.StoredAt });
    }
}
