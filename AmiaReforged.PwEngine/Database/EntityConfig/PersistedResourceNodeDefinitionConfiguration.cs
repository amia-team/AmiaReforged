using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedResourceNodeDefinitionConfiguration : IEntityTypeConfiguration<PersistedResourceNodeDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedResourceNodeDefinition> builder)
    {
        builder.ToTable("ResourceNodeDefinitions");

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
            .HasColumnType("text");

        builder.Property(e => e.PlcAppearance)
            .HasColumnName("plc_appearance");

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .HasDefaultValue("Undefined");

        builder.Property(e => e.Uses)
            .HasColumnName("uses")
            .HasDefaultValue(50);

        builder.Property(e => e.BaseHarvestRounds)
            .HasColumnName("base_harvest_rounds")
            .HasDefaultValue(0);

        builder.Property(e => e.RequirementJson)
            .HasColumnName("requirement")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.OutputsJson)
            .HasColumnName("outputs")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.FloraPropertiesJson)
            .HasColumnName("flora_properties")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Type)
            .HasDatabaseName("IX_ResourceNodeDefinitions_Type");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_ResourceNodeDefinitions_Name");
    }
}
