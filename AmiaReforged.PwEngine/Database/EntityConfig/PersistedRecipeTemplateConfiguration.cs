using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedRecipeTemplateConfiguration : IEntityTypeConfiguration<PersistedRecipeTemplate>
{
    public void Configure(EntityTypeBuilder<PersistedRecipeTemplate> builder)
    {
        builder.ToTable("RecipeTemplateDefinitions");
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

        builder.Property(e => e.IndustryTag)
            .HasColumnName("industry_tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.RequiredKnowledgeJson)
            .HasColumnName("required_knowledge")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.RequiredProficiency)
            .HasColumnName("required_proficiency")
            .HasMaxLength(50);

        builder.Property(e => e.IngredientsJson)
            .HasColumnName("ingredients")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.ProductsJson)
            .HasColumnName("products")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.CraftingTimeSeconds)
            .HasColumnName("crafting_time_seconds");

        builder.Property(e => e.KnowledgePointsAwarded)
            .HasColumnName("knowledge_points_awarded");

        builder.Property(e => e.RequiredWorkstation)
            .HasColumnName("required_workstation")
            .HasMaxLength(128);

        builder.Property(e => e.RequiredToolsJson)
            .HasColumnName("required_tools")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.MetadataJson)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.IndustryTag)
            .HasDatabaseName("IX_RecipeTemplateDefinitions_IndustryTag");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_RecipeTemplateDefinitions_Name");
    }
}
