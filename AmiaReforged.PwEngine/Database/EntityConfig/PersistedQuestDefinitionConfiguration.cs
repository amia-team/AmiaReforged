using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for global quest definitions.
/// </summary>
public sealed class PersistedQuestDefinitionConfiguration : IEntityTypeConfiguration<PersistedQuestDefinition>
{
    public void Configure(EntityTypeBuilder<PersistedQuestDefinition> builder)
    {
        builder.ToTable("codex_quest_definitions");

        builder.HasKey(d => d.QuestId)
            .HasName("codex_quest_definitions_pkey");

        builder.Property(d => d.QuestId)
            .HasColumnName("quest_id")
            .HasMaxLength(100)
            .ValueGeneratedNever();

        builder.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(d => d.StagesJson)
            .HasColumnName("stages_json")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.QuestGiver)
            .HasColumnName("quest_giver")
            .HasMaxLength(200);

        builder.Property(d => d.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(d => d.Keywords)
            .HasColumnName("keywords")
            .HasMaxLength(1000);

        builder.Property(d => d.IsAlwaysAvailable)
            .HasColumnName("is_always_available")
            .HasDefaultValue(false);

        builder.Property(d => d.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Index for loading always-available entries efficiently
        builder.HasIndex(d => d.IsAlwaysAvailable)
            .HasDatabaseName("codex_quest_definitions_always_available_idx");
    }
}
