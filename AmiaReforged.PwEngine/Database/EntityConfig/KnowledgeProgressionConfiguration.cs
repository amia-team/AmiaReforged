using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class KnowledgeProgressionConfiguration : IEntityTypeConfiguration<KnowledgeProgression>
{
    public void Configure(EntityTypeBuilder<KnowledgeProgression> builder)
    {
        builder.ToTable("KnowledgeProgressions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.CharacterId)
            .HasColumnName("character_id")
            .IsRequired();

        builder.HasIndex(e => e.CharacterId)
            .IsUnique();

        builder.Property(e => e.EconomyEarnedKnowledgePoints)
            .HasColumnName("economy_earned_knowledge_points")
            .HasDefaultValue(0);

        builder.Property(e => e.LevelUpKnowledgePoints)
            .HasColumnName("level_up_knowledge_points")
            .HasDefaultValue(0);

        builder.Property(e => e.AccumulatedProgressionPoints)
            .HasColumnName("accumulated_progression_points")
            .HasDefaultValue(0);

        builder.Property(e => e.CapProfileTag)
            .HasColumnName("cap_profile_tag")
            .HasMaxLength(128);

        builder.Ignore(e => e.TotalKnowledgePoints);
    }
}
