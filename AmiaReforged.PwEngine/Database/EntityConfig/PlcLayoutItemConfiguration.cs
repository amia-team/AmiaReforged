using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PlcLayoutItemConfiguration : IEntityTypeConfiguration<PlcLayoutItem>
{
    public void Configure(EntityTypeBuilder<PlcLayoutItem> builder)
    {
        builder.ToTable("plc_layout_items");

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.LayoutConfigurationId)
            .HasColumnName("layout_configuration_id")
            .IsRequired();

        builder.Property(p => p.PlcResRef)
            .HasColumnName("plc_resref")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.PlcName)
            .HasColumnName("plc_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(p => p.X)
            .HasColumnName("x");

        builder.Property(p => p.Y)
            .HasColumnName("y");

        builder.Property(p => p.Z)
            .HasColumnName("z");

        builder.Property(p => p.Orientation)
            .HasColumnName("orientation");

        builder.Property(p => p.Scale)
            .HasColumnName("scale");

        builder.Property(p => p.Appearance)
            .HasColumnName("appearance");

        builder.Property(p => p.HealthOverride)
            .HasColumnName("health_override");

        builder.Property(p => p.IsPlot)
            .HasColumnName("is_plot");

        builder.Property(p => p.IsStatic)
            .HasColumnName("is_static");

        builder.HasIndex(p => p.LayoutConfigurationId)
            .HasDatabaseName("plc_layout_items_layout_id_idx");
    }
}
