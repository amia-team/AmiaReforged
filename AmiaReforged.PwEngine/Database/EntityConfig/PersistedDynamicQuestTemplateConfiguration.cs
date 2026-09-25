using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public sealed class PersistedDynamicQuestTemplateConfiguration
    : IEntityTypeConfiguration<PersistedDynamicQuestTemplate>
{
    public void Configure(
        EntityTypeBuilder<PersistedDynamicQuestTemplate> builder)
    {
        builder.ToTable("dynamic_quest_templates");

        builder.HasKey(x => x.TemplateId);

        builder.Property(x => x.TemplateId)
            .HasColumnName("template_id")
            .ValueGeneratedNever();

        builder.Property(x => x.Source)
            .HasColumnName("source");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => new { x.IsActive, x.Source });
    }
}

public sealed class PersistedDynamicQuestPostingConfiguration
    : IEntityTypeConfiguration<PersistedDynamicQuestPosting>
{
    public void Configure(
        EntityTypeBuilder<PersistedDynamicQuestPosting> builder)
    {
        builder.ToTable("dynamic_quest_postings");

        builder.HasKey(x => x.PostingId);

        builder.Property(x => x.PostingId)
            .HasColumnName("posting_id")
            .ValueGeneratedNever();

        builder.Property(x => x.SourceTemplateId)
            .HasColumnName("source_template_id");

        builder.Property(x => x.PostedAt)
            .HasColumnName("posted_at");

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(x => x.SourceTemplateId);
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne<PersistedDynamicQuestTemplate>()
            .WithMany()
            .HasForeignKey(x => x.SourceTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PersistedDynamicQuestCompletionConfiguration
    : IEntityTypeConfiguration<PersistedDynamicQuestCompletion>
{
    public void Configure(
        EntityTypeBuilder<PersistedDynamicQuestCompletion> builder)
    {
        builder.ToTable("dynamic_quest_completions");

        builder.HasKey(x => new
        {
            x.CharacterId,
            x.TemplateId
        });

        builder.Property(x => x.CharacterId)
            .HasColumnName("character_id");

        builder.Property(x => x.TemplateId)
            .HasColumnName("template_id");

        builder.Property(x => x.CompletionCount)
            .HasColumnName("completion_count");

        builder.Property(x => x.LastCompletedAt)
            .HasColumnName("last_completed_at");

        builder.HasIndex(x => x.TemplateId);

        builder.HasOne<PersistedDynamicQuestTemplate>()
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
