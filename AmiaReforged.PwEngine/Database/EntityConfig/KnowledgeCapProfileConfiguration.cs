using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class KnowledgeCapProfileConfiguration : IEntityTypeConfiguration<KnowledgeCapProfile>
{
    public void Configure(EntityTypeBuilder<KnowledgeCapProfile> builder)
    {
        builder.ToTable("KnowledgeCapProfiles");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.Tag)
            .HasColumnName("tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(e => e.Tag)
            .IsUnique();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(1024);

        builder.Property(e => e.SoftCap)
            .HasColumnName("soft_cap")
            .HasDefaultValue(100);

        builder.Property(e => e.HardCap)
            .HasColumnName("hard_cap")
            .HasDefaultValue(150);
    }
}
