using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for codex quest entries.
/// </summary>
public sealed class PersistedCodexQuestConfiguration : IEntityTypeConfiguration<PersistedCodexQuest>
{
    public void Configure(EntityTypeBuilder<PersistedCodexQuest> builder)
    {
        builder.ToTable("codex_quests");

        // Composite PK — one quest entry per character
        builder.HasKey(q => new { q.CharacterId, q.QuestId })
            .HasName("codex_quests_pkey");

        builder.Property(q => q.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.Property(q => q.QuestId)
            .HasColumnName("quest_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(q => q.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(q => q.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(q => q.State)
            .HasColumnName("state")
            .IsRequired();

        builder.Property(q => q.CurrentStageId)
            .HasColumnName("current_stage_id")
            .IsRequired();

        builder.Property(q => q.DateStarted)
            .HasColumnName("date_started")
            .IsRequired();

        builder.Property(q => q.DateCompleted)
            .HasColumnName("date_completed");

        builder.Property(q => q.QuestGiver)
            .HasColumnName("quest_giver")
            .HasMaxLength(200);

        builder.Property(q => q.Location)
            .HasColumnName("location")
            .HasMaxLength(200);

        builder.Property(q => q.Keywords)
            .HasColumnName("keywords")
            .HasMaxLength(1000);

        builder.Property(q => q.StagesJson)
            .HasColumnName("stages_json")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(q => q.SourceTemplateId)
            .HasColumnName("source_template_id")
            .HasMaxLength(100);

        builder.Property(q => q.Deadline)
            .HasColumnName("deadline");

        builder.Property(q => q.ExpiryBehavior)
            .HasColumnName("expiry_behavior");

        builder.Property(q => q.CompletionCount)
            .HasColumnName("completion_count")
            .HasDefaultValue(0);

        // FK to persisted_characters — cascade delete when character is removed
        builder.HasOne<PersistedCharacter>()
            .WithMany()
            .HasForeignKey(q => q.CharacterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("codex_quests_character_id_fkey");

        // Index for fast lookup by character
        builder.HasIndex(q => q.CharacterId)
            .HasDatabaseName("codex_quests_character_id_idx");

        // Composite index for filtering by character + state
        builder.HasIndex(q => new { q.CharacterId, q.State })
            .HasDatabaseName("codex_quests_character_state_idx");
    }
}
