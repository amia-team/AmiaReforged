using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedWorkstationDefinitionConfiguration : IEntityTypeConfiguration<PersistedWorkstationDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedWorkstationDefinition> builder)
    {
        builder.ToTable("WorkstationDefinitions");
        builder.HasKey(e => e.Tag);

        builder.Property(e => e.Tag)
            .HasColumnName("tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(e => e.PlaceableResRef)
            .HasColumnName("placeable_resref")
            .HasMaxLength(64);

        builder.Property(e => e.AppearanceId)
            .HasColumnName("appearance_id");

        builder.Property(e => e.SupportedIndustriesJson)
            .HasColumnName("supported_industries")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_WorkstationDefinitions_Name");
    }
}
