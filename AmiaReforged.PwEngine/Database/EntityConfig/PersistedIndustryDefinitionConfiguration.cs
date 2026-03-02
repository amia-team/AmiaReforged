using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedIndustryDefinitionConfiguration : IEntityTypeConfiguration<PersistedIndustryDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedIndustryDefinition> builder)
    {
        builder.ToTable("IndustryDefinitions");
        builder.HasKey(e => e.Tag);

        builder.Property(e => e.Tag)
            .HasColumnName("tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256);

        builder.Property(e => e.KnowledgeJson)
            .HasColumnName("knowledge")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.RecipesJson)
            .HasColumnName("recipes")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_IndustryDefinitions_Name");
    }
}
