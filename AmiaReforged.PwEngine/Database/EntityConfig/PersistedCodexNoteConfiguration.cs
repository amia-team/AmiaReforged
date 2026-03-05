using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for codex notes.
/// </summary>
public sealed class PersistedCodexNoteConfiguration : IEntityTypeConfiguration<PersistedCodexNote>
{
    public void Configure(EntityTypeBuilder<PersistedCodexNote> builder)
    {
        builder.ToTable("codex_notes");

        builder.HasKey(n => n.Id)
            .HasName("codex_notes_pkey");

        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(n => n.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(200);

        builder.Property(n => n.Content)
            .HasColumnName("content")
            .IsRequired();

        builder.Property(n => n.Category)
            .HasColumnName("category")
            .IsRequired();

        builder.Property(n => n.IsDmNote)
            .HasColumnName("is_dm_note")
            .IsRequired();

        builder.Property(n => n.IsPrivate)
            .HasColumnName("is_private")
            .IsRequired();

        builder.Property(n => n.CreatedUtc)
            .HasColumnName("created_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(n => n.ModifiedUtc)
            .HasColumnName("modified_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // FK to persisted_characters — cascade delete when character is removed
        builder.HasOne<PersistedCharacter>()
            .WithMany()
            .HasForeignKey(n => n.CharacterId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("codex_notes_character_id_fkey");

        // Index for fast lookup by character
        builder.HasIndex(n => n.CharacterId)
            .HasDatabaseName("codex_notes_character_id_idx");

        // Composite index for filtering by character + category
        builder.HasIndex(n => new { n.CharacterId, n.Category })
            .HasDatabaseName("codex_notes_character_category_idx");
    }
}
