using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PlcLayoutConfigurationConfiguration : IEntityTypeConfiguration<PlcLayoutConfiguration>
{
    public void Configure(EntityTypeBuilder<PlcLayoutConfiguration> builder)
    {
        builder.ToTable("plc_layout_configurations");

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.PropertyId)
            .HasColumnName("property_id")
            .IsRequired();

        builder.Property(p => p.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.CreatedUtc)
            .HasColumnName("created_utc");

        builder.Property(p => p.UpdatedUtc)
            .HasColumnName("updated_utc");

        builder.HasMany(p => p.Items)
            .WithOne(i => i.LayoutConfiguration)
            .HasForeignKey(i => i.LayoutConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.PropertyId, p.CharacterId })
            .HasDatabaseName("plc_layout_configurations_property_character_idx");

        builder.HasIndex(p => p.CharacterId)
            .HasDatabaseName("plc_layout_configurations_character_idx");
    }
}
