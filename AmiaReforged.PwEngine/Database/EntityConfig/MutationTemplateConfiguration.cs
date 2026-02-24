using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class MutationTemplateConfiguration : IEntityTypeConfiguration<MutationTemplate>
{
    public void Configure(EntityTypeBuilder<MutationTemplate> builder)
    {
        builder.ToTable("MutationTemplates");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Prefix)
            .HasColumnName("prefix")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasColumnName("description")
            .HasMaxLength(256);

        builder.Property(m => m.SpawnChancePercent)
            .HasColumnName("spawn_chance_percent")
            .HasDefaultValue(10);

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasMany(m => m.Effects)
            .WithOne(e => e.MutationTemplate)
            .HasForeignKey(e => e.MutationTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.Prefix)
            .IsUnique()
            .HasDatabaseName("IX_MutationTemplates_Prefix");
    }
}
