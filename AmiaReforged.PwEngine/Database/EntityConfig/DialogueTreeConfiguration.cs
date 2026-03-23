using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for dialogue tree definitions.
/// </summary>
public sealed class DialogueTreeConfiguration : IEntityTypeConfiguration<PersistedDialogueTree>
{
    public void Configure(EntityTypeBuilder<PersistedDialogueTree> builder)
    {
        builder.ToTable("dialogue_trees");

        builder.HasKey(d => d.DialogueTreeId)
            .HasName("dialogue_trees_pkey");

        builder.Property(d => d.DialogueTreeId)
            .HasColumnName("dialogue_tree_id")
            .HasMaxLength(100)
            .ValueGeneratedNever();

        builder.Property(d => d.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.Description)
            .HasColumnName("description");

        builder.Property(d => d.RootNodeId)
            .HasColumnName("root_node_id")
            .HasMaxLength(36);

        builder.Property(d => d.SpeakerTag)
            .HasColumnName("speaker_tag")
            .HasMaxLength(64);

        builder.Property(d => d.NodesJson)
            .HasColumnName("nodes_json")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(d => d.CreatedUtc)
            .HasColumnName("created_utc")
            .IsRequired();

        builder.Property(d => d.UpdatedUtc)
            .HasColumnName("updated_utc");

        // Index on speaker_tag for NPC lookup
        builder.HasIndex(d => d.SpeakerTag)
            .HasDatabaseName("ix_dialogue_trees_speaker_tag");
    }
}
