using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class NpcShopProductConfiguration : IEntityTypeConfiguration<NpcShopProductRecord>
{
    public void Configure(EntityTypeBuilder<NpcShopProductRecord> builder)
    {
        builder.ToTable("NpcShopProducts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ResRef)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.LocalVariablesJson)
            .HasColumnType("jsonb");

        builder.Property(x => x.AppearanceJson)
            .HasColumnType("jsonb");

        builder.HasIndex(x => new { x.ShopId, x.ResRef })
            .IsUnique();
    }
}
