using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedInteractionDefinitionConfiguration
    : IEntityTypeConfiguration<PersistedInteractionDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedInteractionDefinition> builder)
    {
        builder.ToTable("InteractionDefinitions");
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

        builder.Property(e => e.TargetMode)
            .HasColumnName("target_mode")
            .HasMaxLength(32);

        builder.Property(e => e.BaseRounds)
            .HasColumnName("base_rounds");

        builder.Property(e => e.MinRounds)
            .HasColumnName("min_rounds");

        builder.Property(e => e.ProficiencyReducesRounds)
            .HasColumnName("proficiency_reduces_rounds");

        builder.Property(e => e.RequiresIndustryMembership)
            .HasColumnName("requires_industry_membership");

        builder.Property(e => e.ResponsesJson)
            .HasColumnName("responses")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_InteractionDefinitions_Name");
    }
}
