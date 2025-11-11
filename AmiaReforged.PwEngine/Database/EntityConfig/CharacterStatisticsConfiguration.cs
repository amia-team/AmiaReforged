using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class CharacterStatisticsConfiguration : IEntityTypeConfiguration<CharacterStatistics>
{
    public void Configure(EntityTypeBuilder<CharacterStatistics> builder)
    {
        builder.ToTable("CharacterStatistics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Required properties
        builder.Property(x => x.CharacterId).IsRequired();
        builder.Property(x => x.KnowledgePoints).IsRequired();
        builder.Property(x => x.TimesDied).IsRequired();
        builder.Property(x => x.TimesRankedUp).IsRequired();
        builder.Property(x => x.IndustriesJoined).IsRequired();
        builder.Property(x => x.PlayTime).IsRequired();

        // Index to ensure at most one stats row per character
        // Remove .IsUnique() if you intend to allow multiple rows per character.
        builder.HasIndex(x => x.CharacterId).IsUnique();

        builder.HasOne<PersistedCharacter>()
            .WithOne(c => c.Statistics)
            .HasForeignKey<CharacterStatistics>(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}