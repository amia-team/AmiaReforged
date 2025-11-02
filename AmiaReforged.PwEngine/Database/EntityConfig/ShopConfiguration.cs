using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class ShopConfiguration : IEntityTypeConfiguration<ShopRecord>
{
    public void Configure(EntityTypeBuilder<ShopRecord> builder)
    {
        builder.ToTable("npc_shops");

        builder.Property(s => s.Tag)
            .HasColumnName("tag")
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(s => s.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.ShopkeeperTag)
            .HasColumnName("shopkeeper_tag")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(s => s.Description)
            .HasColumnName("description");

        builder.Property(s => s.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.ManualRestock)
            .HasColumnName("manual_restock")
            .IsRequired();

        builder.Property(s => s.ManualPricing)
            .HasColumnName("manual_pricing")
            .IsRequired();

        builder.Property(s => s.OwnerAccountId)
            .HasColumnName("owner_account_id");

        builder.Property(s => s.OwnerCharacterId)
            .HasColumnName("owner_character_id");

        builder.Property(s => s.OwnerDisplayName)
            .HasColumnName("owner_display_name")
            .HasMaxLength(255);

        builder.Property(s => s.RestockMinMinutes)
            .HasColumnName("restock_min_minutes");

        builder.Property(s => s.RestockMaxMinutes)
            .HasColumnName("restock_max_minutes");

        builder.Property(s => s.NextRestockUtc)
            .HasColumnName("next_restock_utc");

        builder.Property(s => s.VaultBalance)
            .HasColumnName("vault_balance");

        builder.Property(s => s.DefinitionHash)
            .HasColumnName("definition_hash")
            .HasMaxLength(128);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(s => s.Tag)
            .IsUnique()
            .HasDatabaseName("npc_shops_tag_idx");
    }
}
